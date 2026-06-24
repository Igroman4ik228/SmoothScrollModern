using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmoothScrollModern.Shared.Controls;

public enum ListEmptyStateKind
{
    EmptyList,
    SearchNoResults
}

public sealed partial class ListEmptyState : UserControl
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State),
        typeof(ListEmptyStateKind),
        typeof(ListEmptyState),
        new PropertyMetadata(ListEmptyStateKind.EmptyList, OnStateChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(ListEmptyState),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(ListEmptyState),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(
        nameof(IconGlyph),
        typeof(string),
        typeof(ListEmptyState),
        new PropertyMetadata("\uE8A5"));

    public ListEmptyState()
    {
        InitializeComponent();
        ApplyState(State);
    }

    public ListEmptyStateKind State
    {
        get => (ListEmptyStateKind)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        private set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        private set => SetValue(IconGlyphProperty, value);
    }

    private static void OnStateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ListEmptyState emptyState
            && args.NewValue is ListEmptyStateKind state)
        {
            emptyState.ApplyState(state);
        }
    }

    private void ApplyState(ListEmptyStateKind state)
    {
        Title = state switch
        {
            ListEmptyStateKind.SearchNoResults => "Ничего не найдено",
            _ => "Список пуст"
        };

        IconGlyph = state switch
        {
            ListEmptyStateKind.SearchNoResults => "\uE721",
            _ => "\uE8A5"
        };
    }
}
