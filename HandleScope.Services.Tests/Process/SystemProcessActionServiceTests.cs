using FluentAssertions;
using HandleScope.Services.Abstract;
using HandleScope.Services.Process;
using HandleScope.Services.Results;
using HandleScope.Services.Tests.Constants;
using NSubstitute;
using Serilog;
using Xunit;

namespace HandleScope.Services.Tests.Process;

public sealed class SystemProcessActionServiceTests
{
    [Fact]
    public async Task CloseProcessAsync_CurrentProcess_RefusesToTerminate()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var uiText = Substitute.For<IUiText>();
        uiText.Get(UiTextKey.CannotTerminateSelfMessage).Returns(UnitConstants.CannotTerminateThisApplication);
        var service = new SystemProcessActionService(logger, uiText);

        var result = await service.CloseProcessAsync(Environment.ProcessId, CancellationToken.None);

        result.Should().Be(new ProcessActionResult(false, UnitConstants.CannotTerminateThisApplication));
    }

    [Fact]
    public async Task CloseProcessAsync_CancelledBeforeStart_ThrowsOperationCanceledException()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var service = new SystemProcessActionService(logger, Substitute.For<IUiText>());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> act = () => service.CloseProcessAsync(Environment.ProcessId, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CloseProcessAsync_NonexistentProcess_ReportsAlreadyStopped()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var uiText = Substitute.For<IUiText>();
        uiText.Get(UiTextKey.ProcessNoLongerRunningFormat).Returns(UnitConstants.ProcessAlreadyStoppedFormat);
        var service = new SystemProcessActionService(logger, uiText);

        var result = await service.CloseProcessAsync(int.MaxValue, CancellationToken.None);

        result.Should().Be(new ProcessActionResult(true, string.Format(UnitConstants.ProcessAlreadyStoppedFormat, int.MaxValue)));
    }
}
