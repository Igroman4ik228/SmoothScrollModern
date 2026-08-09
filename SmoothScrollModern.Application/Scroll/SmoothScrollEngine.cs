using System.Diagnostics;
using SmoothScrollModern.Core;
using SmoothScrollModern.Input;
using SmoothScrollModern.Settings;
using Windows.System;

namespace SmoothScrollModern.Scroll;

public sealed class SmoothScrollEngine : ISmoothScrollEngine
{
    private const double MaxFrameDeltaSeconds = 0.05;
    private const double RemainderCompletionThreshold = 0.95;
    private const int TargetFrameTimeMs = 8;
    private const int MaxWheelStepsPerFrame = 3;

    private readonly IInputInjectionService _inputInjectionService;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly object _gate = new();
    private CancellationTokenSource? _physicsCancellation;
    private bool _disposed;
    private bool _isPhysicsRunning;
    private ScrollPhysicsOptions _options = ScrollPhysicsOptions.Default;
    private ScrollDeliveryMode _deliveryMode = ScrollDeliveryMode.FineDelta;
    private double _velocityY;
    private double _velocityX;
    private double _verticalOutputRemainder;
    private double _horizontalOutputRemainder;
    private IntPtr _targetWindowHandle;
    private int _targetScreenX;
    private int _targetScreenY;
    private VirtualKey[] _bypassSmoothingVirtualKeys = [];

    public SmoothScrollEngine(IInputInjectionService inputInjectionService)
    {
        _inputInjectionService = inputInjectionService;
    }

    public void EnqueueWheel(
        int delta,
        bool horizontal,
        ScrollSettingsSnapshot settings,
        ScrollDeliveryMode deliveryMode,
        IntPtr targetWindowHandle,
        int screenX,
        int screenY)
    {
        if (delta == 0 || horizontal && !settings.EnableHorizontalScroll)
        {
            return;
        }

        lock (_gate)
        {
            if (_deliveryMode != deliveryMode || IsDifferentTargetWindow(targetWindowHandle))
            {
                ResetMotion();
                _deliveryMode = deliveryMode;
            }

            _targetWindowHandle = targetWindowHandle;
            _targetScreenX = screenX;
            _targetScreenY = screenY;
            _bypassSmoothingVirtualKeys = settings.BypassSmoothingVirtualKeys.ToArray();
            _options = ScrollPhysicsOptions.From(settings, deliveryMode);

            var currentVelocity = horizontal ? _velocityX : _velocityY;
            var impulse = CalculateVelocityImpulse(delta, currentVelocity, _options);
            if (horizontal)
            {
                ApplyDirectionChangeDamping(ref _velocityX, delta, _options.DirectionChangeDamping);
                _velocityX = Math.Clamp(_velocityX + impulse, -_options.MaxVelocity, _options.MaxVelocity);
            }
            else
            {
                ApplyDirectionChangeDamping(ref _velocityY, delta, _options.DirectionChangeDamping);
                _velocityY = Math.Clamp(_velocityY + impulse, -_options.MaxVelocity, _options.MaxVelocity);
            }

            TraceInput(delta, horizontal, impulse);
            EnsurePhysicsStarted();
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            ResetMotion();
            _isPhysicsRunning = false;
            _physicsCancellation?.Cancel();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _physicsCancellation?.Dispose();
        _disposed = true;
    }

    public bool StopIfBypassKeyDown(VirtualKey virtualKey)
    {
        lock (_gate)
        {
            if (!_isPhysicsRunning || !ShortcutKeys.ContainsMatch(_bypassSmoothingVirtualKeys, virtualKey))
            {
                return false;
            }

            ResetMotion();
            _isPhysicsRunning = false;
            _physicsCancellation?.Cancel();
            return true;
        }
    }

    private async Task RunPhysicsAsync(CancellationToken cancellationToken)
    {
        var lastFrameTime = _stopwatch.Elapsed;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var frameDelayMs = GetFrameDelayMs();
                await Task.Delay(frameDelayMs, cancellationToken).ConfigureAwait(false);

                var now = _stopwatch.Elapsed;
                var dt = Math.Min(Math.Max((now - lastFrameTime).TotalSeconds, 0), MaxFrameDeltaSeconds);
                lastFrameTime = now;

                int verticalDelta;
                int horizontalDelta;
                IntPtr targetWindowHandle;
                int targetScreenX;
                int targetScreenY;
                var isComplete = false;

                lock (_gate)
                {
                    var outputY = _velocityY * dt;
                    var outputX = _velocityX * dt;

                    verticalDelta = ConsumeOutputDelta(ref _verticalOutputRemainder, outputY, _options.DeliveryMode);
                    horizontalDelta = ConsumeOutputDelta(ref _horizontalOutputRemainder, outputX, _options.DeliveryMode);

                    ApplyFriction(ref _velocityY, _options.Friction, dt, _options.StopVelocityThreshold);
                    ApplyFriction(ref _velocityX, _options.Friction, dt, _options.StopVelocityThreshold);

                    targetWindowHandle = _targetWindowHandle;
                    targetScreenX = _targetScreenX;
                    targetScreenY = _targetScreenY;

                    TraceFrame(dt, outputY, outputX, verticalDelta, horizontalDelta);

                    if (IsMotionComplete())
                    {
                        ResetMotion();
                        _isPhysicsRunning = false;
                        isComplete = true;
                    }
                }

                SendDelta(verticalDelta, horizontal: false, targetWindowHandle, targetScreenX, targetScreenY);
                SendDelta(horizontalDelta, horizontal: true, targetWindowHandle, targetScreenX, targetScreenY);

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

    private int GetFrameDelayMs()
    {
        lock (_gate)
        {
            return _options.TargetFrameTimeMs;
        }
    }

    private bool IsDifferentTargetWindow(IntPtr targetWindowHandle)
    {
        return _targetWindowHandle != IntPtr.Zero
               && targetWindowHandle != IntPtr.Zero
               && _targetWindowHandle != targetWindowHandle;
    }

    private static double CalculateVelocityImpulse(int delta, double currentVelocity, ScrollPhysicsOptions options)
    {
        var speedRatio = Math.Clamp(Math.Abs(currentVelocity) / options.MaxVelocity, 0.0, 1.0);
        var burstBoost = 1.0 + (speedRatio * options.BurstAcceleration);
        if (Math.Abs(delta) < Constants.WheelDelta)
        {
            burstBoost = Math.Min(burstBoost, 1.2);
        }

        var targetDistance = delta * options.DistanceMultiplier * options.PrecisionMultiplier * burstBoost;
        return targetDistance * options.Friction;
    }

    private static void ApplyDirectionChangeDamping(ref double velocity, int inputDelta, double damping)
    {
        if (Math.Sign(velocity) != 0 && Math.Sign(velocity) != Math.Sign(inputDelta))
        {
            velocity *= damping;
        }
    }

    private static void ApplyFriction(ref double velocity, double friction, double dt, double stopVelocityThreshold)
    {
        if (Math.Abs(velocity) < stopVelocityThreshold)
        {
            velocity = 0;
            return;
        }

        velocity *= Math.Exp(-friction * dt);
        if (Math.Abs(velocity) < stopVelocityThreshold)
        {
            velocity = 0;
        }
    }

    private static int ConsumeOutputDelta(ref double remainder, double frameDelta, ScrollDeliveryMode deliveryMode)
    {
        remainder += frameDelta;
        return deliveryMode == ScrollDeliveryMode.WheelStep
            ? ConsumeWheelStepDelta(ref remainder)
            : ExtractIntegerDelta(ref remainder);
    }

    private static int ExtractIntegerDelta(ref double remainder)
    {
        if (Math.Abs(remainder) < 1)
        {
            return 0;
        }

        var value = (int)Math.Truncate(remainder);
        remainder -= value;
        return value;
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

    private void SendDelta(int delta, bool horizontal, IntPtr targetWindowHandle, int screenX, int screenY)
    {
        if (delta != 0)
        {
            _inputInjectionService.SendWheel(delta, horizontal, targetWindowHandle, screenX, screenY);
        }
    }

    private bool IsMotionComplete()
    {
        var remainderThreshold = _options.DeliveryMode == ScrollDeliveryMode.WheelStep
            ? Constants.WheelDelta
            : RemainderCompletionThreshold;

        return Math.Abs(_velocityY) < _options.StopVelocityThreshold
               && Math.Abs(_velocityX) < _options.StopVelocityThreshold
               && Math.Abs(_verticalOutputRemainder) < remainderThreshold
               && Math.Abs(_horizontalOutputRemainder) < remainderThreshold;
    }

    private void EnsurePhysicsStarted()
    {
        if (_isPhysicsRunning)
        {
            return;
        }

        _physicsCancellation?.Dispose();
        _physicsCancellation = new CancellationTokenSource();
        _isPhysicsRunning = true;
        _ = RunPhysicsAsync(_physicsCancellation.Token);
    }

    private void ResetMotion()
    {
        _velocityX = 0;
        _velocityY = 0;
        _horizontalOutputRemainder = 0;
        _verticalOutputRemainder = 0;
        _targetWindowHandle = IntPtr.Zero;
        _targetScreenX = 0;
        _targetScreenY = 0;
        _bypassSmoothingVirtualKeys = [];
    }

    [Conditional("DEBUG")]
    private void TraceInput(int delta, bool horizontal, double impulse)
    {
        Debug.WriteLine(
            $"SmoothScroll input delta={delta}, horizontal={horizontal}, impulse={impulse:0.###}, " +
            $"velocityX={_velocityX:0.###}, velocityY={_velocityY:0.###}, " +
            $"deliveryMode={_options.DeliveryMode}, target=0x{_targetWindowHandle.ToInt64():X}");
    }

    [Conditional("DEBUG")]
    private void TraceFrame(double dt, double outputY, double outputX, int intDeltaY, int intDeltaX)
    {
        Debug.WriteLine(
            $"SmoothScroll frame dt={dt:0.0000}, outputY={outputY:0.###}, outputX={outputX:0.###}, " +
            $"intDeltaY={intDeltaY}, intDeltaX={intDeltaX}, velocityX={_velocityX:0.###}, " +
            $"velocityY={_velocityY:0.###}, deliveryMode={_options.DeliveryMode}, " +
            $"target=0x{_targetWindowHandle.ToInt64():X}");
    }

    private readonly record struct ScrollPhysicsOptions(
        int TargetFrameTimeMs,
        double DistanceMultiplier,
        double Friction,
        double BurstAcceleration,
        double MaxVelocity,
        double DirectionChangeDamping,
        double StopVelocityThreshold,
        double PrecisionMultiplier,
        ScrollDeliveryMode DeliveryMode)
    {
        public static ScrollPhysicsOptions Default { get; } = new(
            8,
            1.4,
            18.0,
            1.0,
            7000.0,
            0.18,
            8.0,
            1.0,
            ScrollDeliveryMode.FineDelta);

        public static ScrollPhysicsOptions From(ScrollSettingsSnapshot settings, ScrollDeliveryMode deliveryMode)
        {
            return new ScrollPhysicsOptions(
                SmoothScrollEngine.TargetFrameTimeMs,
                settings.DistanceMultiplier,
                settings.Friction,
                settings.BurstAcceleration,
                settings.MaxVelocity,
                settings.DirectionChangeDamping,
                settings.StopVelocityThreshold,
                settings.PrecisionMultiplier,
                deliveryMode);
        }
    }
}
