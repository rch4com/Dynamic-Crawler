using DynamicCrawler.Core.Common;
using FluentAssertions;

namespace DynamicCrawler.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        var result = Result<int>.Failure("에러 발생", "ERR_001");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
        result.Error.Should().Be("에러 발생");
        result.ErrorCode.Should().Be("ERR_001");
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Success(10);
        var mapped = result.Map(v => v * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(20);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        var result = Result<int>.Failure("실패", "FAIL");
        var mapped = result.Map(v => v.ToString());

        mapped.IsSuccess.Should().BeFalse();
        mapped.Error.Should().Be("실패");
        mapped.ErrorCode.Should().Be("FAIL");
    }

    [Fact]
    public async Task MapAsync_OnSuccess_ShouldTransformValueAsync()
    {
        var result = Result<int>.Success(5);
        var mapped = await result.MapAsync(v => Task.FromResult(v + 10));

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(15);
    }
}
