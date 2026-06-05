using System.Windows;
using HandleScope.Application.Services;
using HandleScope.Infrastructure.Native;
using HandleScope.Infrastructure.Process;
using HandleScope.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HandleScope
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(
                    "logs/handlescope-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            Log.Information("HandleScope starting");

            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("HandleScope shutting down");
            _serviceProvider?.Dispose();
            Log.CloseAndFlush();

            base.OnExit(e);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Log.Logger);
            services.AddSingleton<IHandleSearchService, RestartManagerHandleSearchService>();
            services.AddSingleton<IProcessInfoService, SystemProcessInfoService>();
            services.AddSingleton<IProcessActionService, SystemProcessActionService>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}
