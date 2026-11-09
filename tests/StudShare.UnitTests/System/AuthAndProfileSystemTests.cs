using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudShare.Api.DTos;
using Xunit;

namespace StudShare.UnitTests.System;

public class AuthAndProfileSystemTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthAndProfileSystemTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_ShouldReturn200Ok()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FullAuthFlow_Register_Login_GetMe_UpdateProfile()
    {
        var email = $"flow_{Guid.NewGuid():N}@kpi.ua";
        const string password = "StrongPassword123!";

        var registerReq = new SignUpRequest(email, password, "Дмитро", "Дяченко", null);
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, regResponse.StatusCode);

        var regDoc = JsonDocument.Parse(await regResponse.Content.ReadAsStringAsync());
        var token = regDoc.RootElement.GetProperty("accessToken").GetString();
        Assert.NotNull(token);

        var dupResponse = await _client.PostAsJsonAsync("/api/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Conflict, dupResponse.StatusCode);

        var failLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInRequest(email, "WrongPass123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, failLogin.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new SignInRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var loginToken = loginDoc.RootElement.GetProperty("accessToken").GetString();
        Assert.NotNull(loginToken);

        var unauthMe = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthMe.StatusCode);

        using var authedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        authedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginToken);
        var authMe = await _client.SendAsync(authedRequest);
        Assert.Equal(HttpStatusCode.OK, authMe.StatusCode);

        var userProfile = await authMe.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(userProfile);
        Assert.Equal(email, userProfile.Email);
        Assert.Equal("Дмитро", userProfile.FirstName);

        using var patchRequest = new HttpRequestMessage(HttpMethod.Patch, "/api/profile")
        {
            Content = JsonContent.Create(new UpdateProfileRequest("Дмитро", "Оновлений", "Олегович", email))
        };
        patchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginToken);
        var patchResponse = await _client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updatedProfile = await patchResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Оновлений", updatedProfile!.LastName);
        Assert.Equal("Олегович", updatedProfile.MiddleName);
    }
}
