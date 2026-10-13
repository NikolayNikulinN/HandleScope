using HandleScope.Logger.Constants;
using Serilog;
using System.Globalization;

namespace HandleScope.Logger.Configuration;

/// <summary>Configures file logging in the application Logs directory.</summary>
public static class FileLoggerConfigurator
{
    private static readonly object SyncRoot = new();
    private static bool isConfigured;

    /// <summary>Gets the configured application logger.</summary>
    public static ILogger GetLogger()
    {
        EnsureConfigured();
        return Log.Logger;
    }

    /// <summary>Flushes and closes the configured logger.</summary>
    public static void CloseAndFlush()
    {
        lock (SyncRoot)
        {
            Log.CloseAndFlush();
            isConfigured = false;
        }
    }

    private static void EnsureConfigured()
    {
        lock (SyncRoot)
        {
            if (isConfigured)
            {
                return;
            }

            var logDirectoryPath = ResolveLogDirectoryPath();
            var logFilePathPattern = Path.Combine(logDirectoryPath, LogConstants.LogFileName);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(
                    logFilePathPattern,
                    outputTemplate: LogConstants.OutputTemplate,
                    formatProvider: CultureInfo.InvariantCulture,
                    shared: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: LogConstants.RetainedFileCountLimit)
                .CreateLogger();

            isConfigured = true;
        }
    }

    private static string ResolveLogDirectoryPath()
    {
        var applicationLogDirectory = Path.Combine(FindApplicationRoot(), LogConstants.LogDirectoryName);

        if (CanWriteToDirectory(applicationLogDirectory))
        {
            return applicationLogDirectory;
        }

        Directory.CreateDirectory(LogConstants.LocalAppDataLogDirectory);
        return LogConstants.LocalAppDataLogDirectory;
    }

    private static string FindApplicationRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, LogConstants.SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        return AppContext.BaseDirectory;
    }

    private static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
            var probeFileName = string.Format(
                CultureInfo.InvariantCulture,
                LogConstants.WriteProbeFileNameFormat,
                Guid.NewGuid());
            var probeFilePath = Path.Combine(directoryPath, probeFileName);

            using (File.Create(probeFilePath))
            {
            }

            File.Delete(probeFilePath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
