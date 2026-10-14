using FluentAssertions;
using HandleScope.Services.Abstract;
using HandleScope.Services.Enums;
using HandleScope.Services.Models;
using HandleScope.Services.Native;
using HandleScope.Services.Tests.Constants;
using NSubstitute;
using Serilog;
using Xunit;

namespace HandleScope.Services.Tests.Native;

public sealed class RestartManagerHandleSearchServiceTests
{
    [Fact]
    public void SearchAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var service = new RestartManagerHandleSearchService(Substitute.For<IProcessInfoService>(), logger, Substitute.For<IUiText>());

        Action act = () => { _ = service.SearchAsync(null!, CancellationToken.None); };

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_CancelledBeforeStart_ThrowsOperationCanceledException()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var service = new RestartManagerHandleSearchService(Substitute.For<IProcessInfoService>(), logger, Substitute.For<IUiText>());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var request = new HandleSearchRequest(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(UnitConstants.GuidFormat)), HandleSearchMode.File, false);

        Func<Task> act = () => service.SearchAsync(request, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(HandleSearchMode.File, UiTextKey.TargetFileMissingWarning)]
    [InlineData(HandleSearchMode.Directory, UiTextKey.TargetFolderMissingWarning)]
    public async Task SearchAsync_MissingTarget_ReturnsWarningWithoutResults(HandleSearchMode mode, UiTextKey warningKey)
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var uiText = Substitute.For<IUiText>();
        uiText.Get(warningKey).Returns(UnitConstants.TargetDoesNotExist);
        var service = new RestartManagerHandleSearchService(Substitute.For<IProcessInfoService>(), logger, uiText);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(UnitConstants.GuidFormat));
        var request = new HandleSearchRequest(path, mode, false);

        var result = await service.SearchAsync(request, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Warnings.Should().ContainSingle().Which.Should().Be(UnitConstants.TargetDoesNotExist);
    }

    [Fact]
    public async Task SearchAsync_EmptyDirectory_ReturnsEmptyFolderWarning()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var uiText = Substitute.For<IUiText>();
        uiText.Get(UiTextKey.EmptyFolderWarning).Returns(UnitConstants.FolderIsEmpty);
        var service = new RestartManagerHandleSearchService(Substitute.For<IProcessInfoService>(), logger, uiText);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(UnitConstants.GuidFormat));
        Directory.CreateDirectory(path);

        try
        {
            var request = new HandleSearchRequest(path, HandleSearchMode.Directory, true);

            var result = await service.SearchAsync(request, CancellationToken.None);

            result.Items.Should().BeEmpty();
            result.Warnings.Should().ContainSingle().Which.Should().Be(UnitConstants.FolderIsEmpty);
        }
        finally
        {
            Directory.Delete(path);
        }
    }
}
