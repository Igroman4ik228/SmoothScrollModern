namespace SmoothScrollModern.Scroll;

public interface IScrollDecisionService
{
    ScrollDecision Decide(IntPtr targetWindowHandle);
}
