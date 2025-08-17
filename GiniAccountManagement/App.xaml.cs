using Microsoft.Extensions.Logging;

namespace GiniAccountManagement
{
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;
        public App(ILogger<App> logger)
        {
            InitializeComponent();
            _logger = logger;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            MainPage = new AppShell();
        }
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.ExceptionObject as Exception, "Unhandled exception");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        }
    }
}
