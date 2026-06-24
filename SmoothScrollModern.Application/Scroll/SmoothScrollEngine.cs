using SmoothScrollModern.Input;
using SmoothScrollModern.Core;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

public sealed class SmoothScrollEngine : ISmoothScrollEngine
{
    private const int FrameMs = 8;
    private const int MinimumStep = 1;
    private const int MaxSingleEventDelta = 720;
    private const int MaxBurstLevel = 8;
    private const int MaxWheelStepsPerFrame = 1;
    private const double BurstAccelerationStep = 0.14;
    private const double BurstDrainStep = 0.08;
    private readonly IInputInjectionService _inputInjectionService;
    private readonly object _gate = new();
    private CancellationTokenSource? _animationCancellation;
    private bool _disposed;
    private bool _isAnimating;
    private DateTimeOffset _lastInputAt = DateTimeOffset.MinValue;
    private ScrollAnimationOptions _options = ScrollAnimationOptions.Default;
    private ScrollDeliveryMode _deliveryMode = ScrollDeliveryMode.FineDelta;
    private int _burstLevel;
    private double _remainingVerticalDelta;
    private double _remainingHorizontalDelta;
    private double _verticalOutputRemainder;
    private double _horizontalOutputRemainder;

    public SmoothScrollEngine(IInputInjectionService inputInjectionService)
    {
        _inputInjectionService = inputInjectionService;
    }

    public void EnqueueWheel(int delta, bool horizontal, ScrollSettings settings, ScrollDeliveryMode deliveryMode)
    {
        if (horizontal && !settings.EnableHorizontalScroll)
        {
            return;
        }

        settings.Validate();
        var now = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            if (_deliveryMode != deliveryMode)
            {
                ResetPendingDeltas();
                _deliveryMode = deliveryMode;
            }

            var isBurst = _isAnimating
                          && now - _lastInputAt <= TimeSpan.FromMilliseconds(Math.Max(80, settings.DurationMs));

            _burstLevel = isBurst ? Math.Min(_burstLevel + 1, MaxBurstLevel) : 0;
            _lastInputAt = now;
            _options = ScrollAnimationOptions.From(settings, _burstLevel, deliveryMode);

            var accelerationBoost = 1.0 + (_burstLevel * BurstAccelerationStep * settings.Acceleration);
            var scaledDelta = NormalizeScaledDelta(
                Math.Clamp(
                delta * settings.ScrollMultiplier * settings.Acceleration * accelerationBoost,
                -MaxSingleEventDelta,
                MaxSingleEventDelta),
                deliveryMode);

            if (horizontal)
            {
                _remainingHorizontalDelta += scaledDelta;
            }
            else
            {
                _remainingVerticalDelta += scaledDelta;
            }

            if (!_isAnimating)
            {
                _animationCancellation?.Dispose();
                _animationCancellation = new CancellationTokenSource();
                _isAnimating = true;
                _ = RunAnimationAsync(_animationCancellation.Token);
            }
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            ResetPendingDeltas();
            _isAnimating = false;
            _animationCancellation?.Cancel();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _animationCancellation?.Dispose();
        _disposed = true;
    }

    private async Task RunAnimationAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var options = GetOptions();
                await Task.Delay(options.FrameDelayMs, cancellationToken).ConfigureAwait(false);

                int verticalDelta;
                int horizontalDelta;
                var isComplete = false;

                lock (_gate)
                {
                    var verticalStep = CalculateFrameDelta(_remainingVerticalDelta, _options);
                    var horizontalStep = CalculateFrameDelta(_remainingHorizontalDelta, _options);

                    _remainingVerticalDelta -= verticalStep;
                    _remainingHorizontalDelta -= horizontalStep;

                    verticalDelta = ConsumeOutputDelta(ref _verticalOutputRemainder, verticalStep, _options.DeliveryMode);
                    horizontalDelta = ConsumeOutputDelta(ref _horizontalOutputRemainder, horizontalStep, _options.DeliveryMode);

                    if (_burstLevel > 0)
                    {
                        _burstLevel--;
                        _options = _options with { BurstLevel = _burstLevel };
                    }

                    if (IsAnimationComplete())
                    {
                        _isAnimating = false;
                        _burstLevel = 0;
                        isComplete = true;
                    }
                }

                SendDelta(verticalDelta, horizontal: false);
                SendDelta(horizontalDelta, horizontal: true);

                if (isComplete)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private ScrollAnimationOptions GetOptions()
    {
        lock (_gate)
        {
            return _options;
        }
    }

    private static double CalculateFrameDelta(double remainingDelta, ScrollAnimationOptions options)
    {
        if (Math.Abs(remainingDelta) < 0.001)
        {
            return 0;
        }

        var frameProgress = options.FrameDelayMs / (double)options.DurationMs;
        var easedProgress = EasingFunctions.Apply(options.EasingType, frameProgress);
        var smoothnessFactor = 1.0 + ((1.0 - options.Smoothness) * 0.55);
        var burstFactor = 1.0 + (options.BurstLevel * BurstDrainStep);
        var frameFactor = Math.Clamp(easedProgress * smoothnessFactor * burstFactor, 0.045, 0.65);

        return remainingDelta * frameFactor;
    }

    private static double NormalizeScaledDelta(double scaledDelta, ScrollDeliveryMode deliveryMode)
    {
        if (deliveryMode == ScrollDeliveryMode.FineDelta || Math.Abs(scaledDelta) < 0.001)
        {
            return scaledDelta;
        }

        var direction = Math.Sign(scaledDelta);
        var steps = Math.Max(1, (int)Math.Round(Math.Abs(scaledDelta) / Constants.WheelDelta));
        return direction * Math.Min(steps * Constants.WheelDelta, MaxSingleEventDelta);
    }

    private static int ConsumeOutputDelta(ref double remainder, double frameDelta, ScrollDeliveryMode deliveryMode)
    {
        remainder += frameDelta;

        if (deliveryMode == ScrollDeliveryMode.WheelStep)
        {
            return ConsumeWheelStepDelta(ref remainder);
        }

        if (Math.Abs(remainder) < MinimumStep)
        {
            return 0;
        }

        var delta = (int)Math.Truncate(remainder);
        delta = Math.Clamp(delta, -Constants.WheelDelta, Constants.WheelDelta);
        remainder -= delta;
        return delta;
    }

    private static int ConsumeWheelStepDelta(ref double remainder)
    {
        if (Math.Abs(remainder) < Constants.WheelDelta)
        {
            return 0;
        }

        var direction = Math.Sign(remainder);
        var availableSteps = (int)(Math.Abs(remainder) / Constants.WheelDelta);
        var steps = Math.Min(availableSteps, MaxWheelStepsPerFrame);
        var delta = direction * Constants.WheelDelta * steps;
        remainder -= delta;
        return delta;
    }

    private void SendDelta(int delta, bool horizontal)
    {
        if (delta != 0)
        {
            _inputInjectionService.SendWheel(delta, horizontal);
        }
    }

    private bool IsAnimationComplete()
    {
        var remainderThreshold = _options.DeliveryMode == ScrollDeliveryMode.WheelStep
            ? Constants.WheelDelta
            : MinimumStep;

        return Math.Abs(_remainingVerticalDelta) < 0.5
               && Math.Abs(_remainingHorizontalDelta) < 0.5
               && Math.Abs(_verticalOutputRemainder) < remainderThreshold
               && Math.Abs(_horizontalOutputRemainder) < remainderThreshold;
    }

    private void ResetPendingDeltas()
    {
        _remainingHorizontalDelta = 0;
        _remainingVerticalDelta = 0;
        _horizontalOutputRemainder = 0;
        _verticalOutputRemainder = 0;
        _burstLevel = 0;
    }

    private readonly record struct ScrollAnimationOptions(
        int DurationMs,
        int FrameDelayMs,
        double Smoothness,
        EasingType EasingType,
        int BurstLevel,
        ScrollDeliveryMode DeliveryMode)
    {
        public static ScrollAnimationOptions Default { get; } = new(
            160,
            FrameMs,
            0.75,
            EasingType.EaseOutCubic,
            0,
            ScrollDeliveryMode.FineDelta);

        public static ScrollAnimationOptions From(ScrollSettings settings, int burstLevel, ScrollDeliveryMode deliveryMode)
        {
            var smoothness = Math.Clamp(settings.Smoothness, 0.0, 1.0);
            var frameDelay = Math.Max(FrameMs, (int)Math.Round(FrameMs + ((1.0 - smoothness) * FrameMs)));

            return new ScrollAnimationOptions(
                Math.Max(settings.DurationMs, FrameMs),
                frameDelay,
                smoothness,
                settings.EasingType,
                burstLevel,
                deliveryMode);
        }
    }
}
