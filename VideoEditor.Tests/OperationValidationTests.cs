using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class OperationValidationTests
{
    [Fact]
    public void TrimRequest_InvalidRange_ReturnsValidationErrors()
    {
        var request = new TrimRequest("in.mp4", "out.mp4", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        var errors = request.Validate();

        Assert.Contains("End must be greater than Start.", errors);
    }

    [Fact]
    public void WatermarkOverlayRequest_MissingImageAndText_ReturnsValidationErrors()
    {
        var request = new WatermarkOverlayRequest("in.mp4", "out.mp4", null, null, "10:20");

        var errors = request.Validate();

        Assert.Contains("Provide WatermarkImagePath or WatermarkText.", errors);
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
