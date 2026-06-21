using System.ComponentModel;

namespace SmoothScrollModern.Native;

public sealed class WinApiException : Win32Exception
{
    public WinApiException(string operation)
        : base($"{operation} failed with Win32 error.")
    {
        Operation = operation;
    }

    public string Operation { get; }
}
