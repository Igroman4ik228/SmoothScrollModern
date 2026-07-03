using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Features.Applications.ViewModels;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Shared.Controls;

namespace SmoothScrollModern.Features.Applications.Controls;

public sealed partial class ApplicationRuleItem : UserControl
{
    public static readonly DependencyProperty AreSettingsVisibleProperty = DependencyProperty.Register(
        nameof(AreSettingsVisible),
        typeof(bool),
        typeof(ApplicationRuleItem),
        new PropertyMetadata(false, OnAreSettingsVisibleChanged));

    public ApplicationRuleItem()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public bool AreSettingsVisible
    {
        get => (bool)GetValue(AreSettingsVisibleProperty);
        set => SetValue(AreSettingsVisibleProperty, value);
    }

    private static void OnAreSettingsVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ApplicationRuleItem { AreSettingsVisible: true } item)
        {
            item.EnsureRuleSettings();
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (RuleSettingsHost.Content is FrameworkElement settings)
        {
            settings.DataContext = args.NewValue;
        }
    }

    private void EnsureRuleSettings()
    {
        if (RuleSettingsHost.Content is ApplicationRuleSettings)
        {
            return;
        }

        var ruleSettings = new ApplicationRuleSettings
        {
            DataContext = DataContext,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (VisualTreeDataContext.FindAncestor<ApplicationRulesViewModel>(this) is not { } viewModel)
        {
            return;
        }

        ruleSettings.DeliveryModeOptions = viewModel.DeliveryModeOptions;
        ruleSettings.ScrollProfileChoices = viewModel.ScrollProfileChoices;
        RuleSettingsHost.Content = ruleSettings;
    }

    private void OnRuleExpanderExpanded(object? sender, EventArgs e)
    {
        EnsureRuleSettings();
    }

    private void OnRemoveRuleClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ApplicationRule rule)
        {
            return;
        }

        var viewModel = VisualTreeDataContext.FindAncestor<ApplicationRulesViewModel>(this);
        if (viewModel?.RemoveRuleCommand.CanExecute(rule) == true)
        {
            viewModel.RemoveRuleCommand.Execute(rule);
        }
    }
}
