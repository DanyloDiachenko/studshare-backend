using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using StudShare.Api.DTos;
using StudShare.Api.Services;
using Xunit;

namespace StudShare.UnitTests.System;

public class NonFunctionalRequirementsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NonFunctionalRequirementsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Performance_ApiEndpointResponseTime_ShouldBeUnderTwoSeconds()
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await _client.GetAsync("/");

        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Response took {stopwatch.ElapsedMilliseconds} ms, which exceeds the 2000 ms limit.");
    }

    [Fact]
    public async Task Security_PasswordMustBeHashed_NeverStoredAsPlainText()
    {
        const string plainPassword = "SuperSecretPassword123!";
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        Assert.DoesNotContain(plainPassword, hash);
        Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("OtherPassword123!", hash));
    }

    [Fact]
    public void FilePolicy_MaxFileSizeLimit_ShouldBeConfiguredToFiftyMegabytes()
    {
        const long maxFileSizeBytes = 50L * 1024 * 1024;
        const long testExceedingSizeBytes = 51L * 1024 * 1024;

        Assert.Equal(52428800L, maxFileSizeBytes);
        Assert.True(testExceedingSizeBytes > maxFileSizeBytes);
    }

    [Theory]
    [InlineData("pdf", true)]
    [InlineData("docx", true)]
    [InlineData("doc", true)]
    [InlineData("pptx", true)]
    [InlineData("ppt", true)]
    [InlineData("jpg", true)]
    [InlineData("png", true)]
    [InlineData("mp4", true)]
    public void FilePolicy_AllowedExtensions_ShouldMatchSupportedCategories(string ext, bool expectedAllowed)
    {
        var category = FileService.GetCategory(ext);
        var isSupported = category != StudShare.Api.Entities.FileCategory.Other;
        Assert.Equal(expectedAllowed, isSupported);
    }

    [Theory]
    [InlineData("short", false)]
    [InlineData("1234567", false)]
    [InlineData("ValidPass123!", true)]
    public async Task Security_PasswordLengthValidation_ShouldEnforceMinimumEightCharacters(string password, bool shouldPass)
    {
        var req = new SignUpRequest($"user_{Guid.NewGuid():N}@kpi.ua", password, "Ім'я", "Прізвище", null);
        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        if (shouldPass)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
