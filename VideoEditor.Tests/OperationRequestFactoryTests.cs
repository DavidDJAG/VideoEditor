using VideoEditor.Application.Services;
using VideoEditor.Domain.Models;

namespace VideoEditor.Tests;

public sealed class OperationRequestFactoryTests
{
    private readonly OperationRequestFactory _factory = new();

    [Fact]
    public void Create_TrimKind_ReturnsTrimRequest()
    {
        var parameters = new OperationParameters(
            "in.mp4",
            "out.mp4",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            null,
            1.0,
            [],
            new Dictionary<string, string>(),
            null);

        var request = _factory.Create(OperationKind.Trim, parameters);

        var typed = Assert.IsType<TrimRequest>(request);
        Assert.Equal(TimeSpan.FromSeconds(1), typed.Start);
        Assert.Equal(TimeSpan.FromSeconds(3), typed.End);
    }

    [Fact]
    public void Catalog_V1Functional_ContainsCutJoinAndSplitAv()
    {
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.Trim).Phase);
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.Concat).Phase);
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.ExtractAudio).Phase);
        Assert.Equal(OperationPhase.V1Functional, OperationCatalog.Get(OperationKind.ExtractVideo).Phase);
    }
}
