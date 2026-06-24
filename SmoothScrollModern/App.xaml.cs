using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Composition;
using SmoothScrollModern.Core;
using SmoothScrollModern.Input;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Startup;
using SmoothScrollModern.Tray;
using SmoothScrollModern.Widgets.Shell.ViewModels;

namespace SmoothScrollModern
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private AppBootstrapper? _bootstrapper;

        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
        }

        public AppBootstrapper? Bootstrapper => _bootstrapper;

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _serviceProvider = ConfigureServices();
            _bootstrapper = _serviceProvider.GetRequiredService<AppBootstrapper>();
            _bootstrapper.Run();
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _bootstrapper?.Dispose();
            System.Windows.Forms.MessageBox.Show(
                e.Exception.Message,
                Core.Constants.ApplicationName,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton(provider => provider.GetRequiredService<ISettingsService>().Load());

            services.AddSingleton<IActiveWindowService, ActiveWindowService>();
            services.AddSingleton<IApplicationRulesService, ApplicationRulesService>();
            services.AddSingleton<IStartupService, WindowsStartupService>();
            services.AddSingleton<IInputInjectionService, InputInjectionService>();
            services.AddSingleton<ISmoothScrollEngine, SmoothScrollEngine>();
            services.AddSingleton<IMouseHookService, MouseHookService>();
            services.AddSingleton<ITrayService, TrayService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<AppBootstrapper>();

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }
    }
}
