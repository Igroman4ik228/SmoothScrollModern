namespace SmoothScrollModern.ViewModels;

public sealed class SelectionOption<T>
{
    public SelectionOption(T value, string name)
    {
        Value = value;
        Name = name;
    }

    public T Value { get; }

    public string Name { get; }
}
