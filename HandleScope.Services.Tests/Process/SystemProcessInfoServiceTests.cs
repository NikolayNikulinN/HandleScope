using FluentAssertions;
using HandleScope.Services.Abstract;
using HandleScope.Services.Models;
using HandleScope.Services.Process;
using HandleScope.Services.Tests.Constants;
using NSubstitute;
using Serilog;
using Xunit;

namespace HandleScope.Services.Tests.Process;

public sealed class SystemProcessInfoServiceTests
{
    [Fact]
    public void GetProcessDetails_NonexistentProcess_ReturnsFallback()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var uiText = Substitute.For<IUiText>();
        uiText.Get(UiTextKey.ProcessDetailsFallbackNameFormat).Returns(UnitConstants.ProcessFallbackNameFormat);
        var service = new SystemProcessInfoService(logger, uiText);

        var result = service.GetProcessDetails(int.MaxValue);

        result.Should().Be(new ProcessDetails(int.MaxValue, string.Format(UnitConstants.ProcessFallbackNameFormat, int.MaxValue), null, null));
    }

    [Fact]
    public void GetProcessDetails_CurrentProcess_ReturnsItsIdentity()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var service = new SystemProcessInfoService(logger, Substitute.For<IUiText>());
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

        var result = service.GetProcessDetails(currentProcess.Id);

        result.ProcessId.Should().Be(currentProcess.Id);
        result.ProcessName.Should().Be(currentProcess.ProcessName);
    }
}
