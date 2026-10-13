namespace HandleScope.Logger.Constants;

/// <summary>
/// Provides logging configuration and message templates.
/// </summary>
public static class LogConstants
{
    public const int RetainedFileCountLimit = 10;

    public const string ApplicationFolderName = "HandleScope";
    public const string LogDirectoryName = "Logs";

    public const string LogFileName = "HandleScope-.log";

    public const string WriteProbeFileNameFormat = ".write-test-{0:N}.tmp";

    public const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    public const string LogTemplateUnhandledUiException = "Unhandled UI exception.";

    public const string LogTemplateUnhandledApplicationException = "Unhandled application exception.";

    public const string LogTemplateApplicationStarted = "HandleScope started.";

    public const string LogTemplateApplicationExited = "HandleScope exited with code {ExitCode}.";

    public static string LogDirectory { get; } = Path.Combine(
                                                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                               ApplicationFolderName,
                                                               LogDirectoryName);
}
