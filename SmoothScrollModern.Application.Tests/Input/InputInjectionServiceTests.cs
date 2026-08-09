using SmoothScrollModern.Input;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Application.Tests.Input;

public sealed class InputInjectionServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SendWheel_DeliversBothDirectionsToWindowUnderPointerWithinSourceRoot(bool horizontal)
    {
        var sourceRootWindowHandle = (IntPtr)100;
        var pointerWindowHandle = (IntPtr)101;
        var platform = new FakeWheelDeliveryPlatform(
            cursorWindowHandle: pointerWindowHandle,
            roots:
            [
                (sourceRootWindowHandle, sourceRootWindowHandle),
                (pointerWindowHandle, sourceRootWindowHandle)
            ]);
        var service = new InputInjectionService(platform);

        var delivered = service.SendWheel(120, horizontal, sourceRootWindowHandle);

        Assert.True(delivered);
        var request = Assert.Single(platform.PostRequests);
        Assert.Equal(pointerWindowHandle, request.TargetWindowHandle);
        Assert.Equal(120, request.Delta);
        Assert.Equal(horizontal, request.Horizontal);
    }

    [Fact]
    public void SendWheel_CancelsWhenPointerLeavesSourceRootWindow()
    {
        var sourceRootWindowHandle = (IntPtr)100;
        var otherRootWindowHandle = (IntPtr)200;
        var platform = new FakeWheelDeliveryPlatform(
            cursorWindowHandle: otherRootWindowHandle,
            roots:
            [
                (sourceRootWindowHandle, sourceRootWindowHandle),
                (otherRootWindowHandle, otherRootWindowHandle)
            ]);
        var service = new InputInjectionService(platform);

        var delivered = service.SendWheel(120, horizontal: false, sourceRootWindowHandle);

        Assert.False(delivered);
        Assert.Empty(platform.PostRequests);
    }

    [Fact]
    public void SendWheel_CancelsWhenCurrentPointerCannotBeResolved()
    {
        var platform = new FakeWheelDeliveryPlatform(
            cursorWindowHandle: (IntPtr)101,
            roots: [((IntPtr)100, (IntPtr)100)])
        {
            CanGetCursorPosition = false
        };
        var service = new InputInjectionService(platform);

        var delivered = service.SendWheel(120, horizontal: false, (IntPtr)100);

        Assert.False(delivered);
        Assert.Empty(platform.PostRequests);
    }

    [Fact]
    public void SendWheel_CancelsWhenAddressedDeliveryFails()
    {
        var sourceRootWindowHandle = (IntPtr)100;
        var platform = new FakeWheelDeliveryPlatform(
            cursorWindowHandle: sourceRootWindowHandle,
            roots: [(sourceRootWindowHandle, sourceRootWindowHandle)])
        {
            CanPostMessage = false
        };
        var service = new InputInjectionService(platform);

        var delivered = service.SendWheel(120, horizontal: true, sourceRootWindowHandle);

        Assert.False(delivered);
        Assert.Single(platform.PostRequests);
    }

    private sealed class FakeWheelDeliveryPlatform(
        IntPtr cursorWindowHandle,
        IReadOnlyList<(IntPtr WindowHandle, IntPtr RootWindowHandle)> roots) : IWheelDeliveryPlatform
    {
        private readonly Dictionary<IntPtr, IntPtr> _roots = roots.ToDictionary(item => item.WindowHandle, item => item.RootWindowHandle);

        public bool CanGetCursorPosition { get; set; } = true;

        public bool CanPostMessage { get; set; } = true;

        public List<PostRequest> PostRequests { get; } = [];

        public bool TryGetCursorPosition(out POINT cursorPoint)
        {
            cursorPoint = new POINT(640, 480);
            return CanGetCursorPosition;
        }

        public IntPtr GetWindowAt(POINT screenPoint) => cursorWindowHandle;

        public IntPtr GetRootWindow(IntPtr windowHandle)
        {
            return _roots.GetValueOrDefault(windowHandle);
        }

        public bool TryPostWheelMessage(IntPtr targetWindowHandle, int delta, bool horizontal, POINT screenPoint)
        {
            PostRequests.Add(new PostRequest(targetWindowHandle, delta, horizontal));
            return CanPostMessage;
        }
    }

    private sealed record PostRequest(IntPtr TargetWindowHandle, int Delta, bool Horizontal);
}
