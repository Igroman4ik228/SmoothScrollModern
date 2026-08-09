namespace SmoothScrollModern.Applications;

public interface IWindowIdentityResolver
{
    WindowIdentity Resolve(IntPtr windowHandle);
}
