using VidEditor.Domain.Models;

namespace VidEditor.Tests;

public sealed class ContainerCatalogTests
{
    [Fact]
    public void NormalizeId_MapsMatroskaMuxerNameToMkvContainerId()
    {
        Assert.Equal("mkv", ContainerCatalog.NormalizeId("matroska"));
    }

    [Fact]
    public void GetAvailableUserSelectableContainers_FiltersTechnicalMuxersAndKeepsMkv()
    {
        var containers = ContainerCatalog.GetAvailableUserSelectableContainers(["matroska", "mkvtimestamp_v2", "mp4"]);

        Assert.Contains(containers, container => container.Id == "mkv");
        Assert.DoesNotContain(containers, container => container.Id == "mkvtimestamp_v2");
    }

    [Fact]
    public void ResolveMuxerName_ForMkv_ReturnsMatroska()
    {
        Assert.Equal("matroska", ContainerCatalog.ResolveMuxerName("mkv"));
    }
}
