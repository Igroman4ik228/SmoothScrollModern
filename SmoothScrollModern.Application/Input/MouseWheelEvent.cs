namespace SmoothScrollModern.Input;

public sealed record MouseWheelEvent(int Delta, bool IsHorizontal, uint Timestamp);
