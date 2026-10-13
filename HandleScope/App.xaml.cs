using System.Windows;
using HandleScope.Application.Services;
using HandleScope.Domain;
using HandleScope.Infrastructure.Native;
using HandleScope.Infrastructure.Process;
using HandleScope.Presentation.Abstract;
using HandleScope.Presentation.Constants;
using HandleScope.Presentation.Wrappers;
using HandleScope.ViewModels;
using HandleScope.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HandleScope;

/// <summary>Composes the application and manages its lifetime.</summary>
public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(AppStrings.LogFilePath,
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: 14)
            .CreateLogger();
        Log.Information(AppStrings.ApplicationStartingLogMessage);

        var services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();
        Resources[nameof(ResourceWrapper)] = serviceProvider.GetRequiredService<IResourceWrapper>();
        MainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information(AppStrings.ApplicationShuttingDownLogMessage);
        serviceProvider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Log.Logger);
        services.AddSingleton<ResourceWrapper>();
        services.AddSingleton<IResourceWrapper>(provider => provider.GetRequiredService<ResourceWrapper>());
        services.AddSingleton<IUiText>(provider => provider.GetRequiredService<ResourceWrapper>());
        services.AddSingleton<AppConstants>();
        services.AddSingleton<IHandleSearchService, RestartManagerHandleSearchService>();
        services.AddSingleton<IProcessInfoService, SystemProcessInfoService>();
        services.AddSingleton<IProcessActionService, SystemProcessActionService>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}
