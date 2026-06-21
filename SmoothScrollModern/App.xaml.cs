using Microsoft.UI.Xaml;
using SmoothScrollModern.Core;

namespace SmoothScrollModern
{
    public partial class App : Application
    {
        private AppBootstrapper? _bootstrapper;

        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
        }

        public AppBootstrapper? Bootstrapper => _bootstrapper;

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _bootstrapper = new AppBootstrapper();
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
    }
}
