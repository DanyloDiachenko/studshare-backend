using StudShare.Api.Entities;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.Unit;

public class FileMetadataHelperTests
{
    [Theory]
    [InlineData("pdf", FileCategory.Pdf)]
    [InlineData("docx", FileCategory.Docx)]
    [InlineData("doc", FileCategory.Docx)]
    [InlineData("pptx", FileCategory.Pptx)]
    [InlineData("ppt", FileCategory.Pptx)]
    [InlineData("jpg", FileCategory.Image)]
    [InlineData("jpeg", FileCategory.Image)]
    [InlineData("png", FileCategory.Image)]
    [InlineData("webp", FileCategory.Image)]
    [InlineData("mp4", FileCategory.Video)]
    [InlineData("mov", FileCategory.Video)]
    [InlineData("avi", FileCategory.Video)]
    [InlineData("txt", FileCategory.Other)]
    [InlineData("zip", FileCategory.Other)]
    public void GetCategory_ShouldMapExtensionsCorrectly(string ext, FileCategory expected)
    {
        var category = FileService.GetCategory(ext);
        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData("pdf", "application/pdf")]
    [InlineData("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("jpg", "image/jpeg")]
    [InlineData("png", "image/png")]
    [InlineData("mp4", "video/mp4")]
    [InlineData("unknown", "application/octet-stream")]
    public void GetContentType_ShouldReturnCorrectMimeType(string ext, string expectedMime)
    {
        var contentType = FileService.GetContentType(ext);
        Assert.Equal(expectedMime, contentType);
    }
}
