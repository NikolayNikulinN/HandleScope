using System.Windows;
using HandleScope.Services.Abstract;
using HandleScope.Logger.Configuration;
using HandleScope.Logger.Constants;
using HandleScope.Services.Native;
using HandleScope.Services.Process;
using HandleScope.Presentation.Abstract;
using HandleScope.Presentation.Constants;
using HandleScope.Presentation.Wrappers;
using HandleScope.ViewModels;
using HandleScope.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HandleScope;

/// <summary>Composes the application and manages its lifetime.</summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        FileLoggerConfigurator.GetLogger();
        Log.Information(LogConstants.ApplicationStartingLogMessage);

        var services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        Resources[nameof(ResourceWrapper)] = serviceProvider.GetRequiredService<IResourceWrapper>();
        MainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information(LogConstants.ApplicationShuttingDownLogMessage);
        serviceProvider?.Dispose();
        FileLoggerConfigurator.CloseAndFlush();
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
