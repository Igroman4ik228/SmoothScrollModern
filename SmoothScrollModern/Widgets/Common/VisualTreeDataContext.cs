using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace SmoothScrollModern.Widgets.Common;

public static class VisualTreeDataContext
{
    public static T? FindAncestor<T>(DependencyObject source)
        where T : class
    {
        for (DependencyObject? current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { DataContext: T viewModel })
            {
                return viewModel;
            }
        }

        return null;
    }
}
