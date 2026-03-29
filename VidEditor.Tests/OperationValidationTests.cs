using VidEditor.Domain.Models;

namespace VidEditor.Tests;

public sealed class OperationValidationTests
{
    [Fact]
    public void ExtractAudioRequest_EmptyPaths_ReturnsValidationErrors()
    {
        var request = new ExtractAudioRequest("", "");

        var errors = request.Validate();

        Assert.Contains("InputPath is required.", errors);
        Assert.Contains("OutputPath is required.", errors);
    }

    [Fact]
    public void ExtractVideoRequest_EmptyPaths_ReturnsValidationErrors()
    {
        var request = new ExtractVideoRequest(" ", null!);

        var errors = request.Validate();

        Assert.Contains("InputPath is required.", errors);
        Assert.Contains("OutputPath is required.", errors);
    }

    [Fact]
    public void TrimRequest_InvalidRange_ReturnsValidationErrors()
    {
        var request = new TrimRequest("in.mp4", "out.mp4", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        var errors = request.Validate();

        Assert.Contains("End must be greater than Start.", errors);
    }

    [Fact]
    public void TrimRequest_NegativeStart_ReturnsValidationErrors()
    {
        var request = new TrimRequest("in.mp4", "out.mp4", TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(4));

        var errors = request.Validate();

        Assert.Contains("Start must be non-negative.", errors);
    }

    [Fact]
    public void ConcatRequest_InsufficientInputs_ReturnsValidationErrors()
    {
        var request = new ConcatRequest(["single.mp4"], "joined.mp4");

        var errors = request.Validate();

        Assert.Contains("At least two inputs are required.", errors);
    }

    [Fact]
    public void ConcatRequest_ContainsEmptyInputPath_ReturnsValidationErrors()
    {
        var request = new ConcatRequest(["a.mp4", ""], "joined.mp4");

        var errors = request.Validate();

        Assert.Contains("Concat inputs must be a non-empty ordered list.", errors);
    }

    [Fact]
    public void WatermarkOverlayRequest_MissingImageAndText_ReturnsValidationErrors()
    {
        var request = new WatermarkOverlayRequest("in.mp4", "out.mp4", null, null, "10:20");

        var errors = request.Validate();

        Assert.Contains("Provide WatermarkImagePath or WatermarkText.", errors);
    }


    [Fact]
    public void ConvertRequest_TechnicalMuxerIdentifier_ReturnsValidationErrors()
    {
        var request = new ConvertRequest(
            "in.mp4",
            "out.mkv",
            ConvertOptions.CreateBalancedMp4H264() with { Container = "mkvtimestamp_v2" });

        var errors = request.Validate();

        Assert.Contains("Container 'mkvtimestamp_v2' is not supported.", errors);
    }

    [Fact]
    public void OperationValidation_InvalidRequest_ThrowsWithCombinedErrorMessage()
    {
        var request = new SegmentHlsRequest("", "", "", 0);

        var ex = Assert.Throws<InvalidOperationException>(() => OperationValidation.ThrowIfInvalid(request));

        Assert.Contains("InputPath is required.", ex.Message);
        Assert.Contains("OutputPlaylistPath is required.", ex.Message);
        Assert.Contains("SegmentDurationSeconds must be greater than 0.", ex.Message);
    }
}
