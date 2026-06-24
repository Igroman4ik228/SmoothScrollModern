using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.ViewModels;

namespace SmoothScrollModern.Features.Settings.Controls;

public sealed partial class ApplicationPreferencesBlock : UserControl
{
    public ApplicationPreferencesBlock()
    {
        InitializeComponent();
    }

    private async void OnResetDefaultsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || !viewModel.ResetDefaultsCommand.CanExecute(null))
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Сбросить настройки?",
            Content = "Все параметры прокрутки, профили и правила исключений вернутся к значениям по умолчанию.",
            PrimaryButtonText = "Сбросить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            viewModel.ResetDefaultsCommand.Execute(null);
        }
    }
}
