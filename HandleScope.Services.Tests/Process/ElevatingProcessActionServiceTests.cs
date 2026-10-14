using FluentAssertions;
using HandleScope.Services.Abstract;
using HandleScope.Services.Process;
using HandleScope.Services.Results;
using HandleScope.Services.Tests.Constants;
using NSubstitute;
using Serilog;
using Xunit;

namespace HandleScope.Services.Tests.Process;

public sealed class ElevatingProcessActionServiceTests
{
    [Fact]
    public async Task CloseProcessAsync_LocalActionDoesNotRequireElevation_ReturnsLocalResult()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var uiText = Substitute.For<IUiText>();
        uiText.Get(UiTextKey.CannotTerminateSelfMessage).Returns(UnitConstants.CannotTerminateThisApplication);
        var localActionService = new SystemProcessActionService(logger, uiText);
        var service = new ElevatingProcessActionService(localActionService, uiText, logger);

        var result = await service.CloseProcessAsync(Environment.ProcessId, CancellationToken.None);

        result.Should().Be(new ProcessActionResult(false, UnitConstants.CannotTerminateThisApplication));
    }
}
