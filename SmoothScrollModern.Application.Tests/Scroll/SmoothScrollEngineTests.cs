using SmoothScrollModern.Input;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Application.Tests.Scroll;

public sealed class SmoothScrollEngineTests
{
    [Fact]
    public async Task EnqueueWheel_StopsInertiaWhenDeliveryIsRejected()
    {
        var inputInjectionService = new RejectingInputInjectionService();
        using var engine = new SmoothScrollEngine(inputInjectionService);

        engine.EnqueueWheel(
            delta: 120,
            horizontal: false,
            settings: ScrollSettingsSnapshot.From(new ScrollSettings()),
            deliveryMode: ScrollDeliveryMode.FineDelta,
            targetWindowHandle: (IntPtr)100);

        await inputInjectionService.FirstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(50);

        Assert.Equal(1, inputInjectionService.AttemptCount);
    }

    private sealed class RejectingInputInjectionService : IInputInjectionService
    {
        private int _attemptCount;

        public TaskCompletionSource FirstAttempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public bool SendWheel(int delta, bool horizontal, IntPtr targetWindowHandle)
        {
            Interlocked.Increment(ref _attemptCount);
            FirstAttempt.TrySetResult();
            return false;
        }
    }
}
