using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StudShare.Api.DTos;
using Xunit;

namespace StudShare.UnitTests.System;

public class GroupAndMembershipSystemTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GroupAndMembershipSystemTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullGroupLifecycle_CreateGroup_Invite_AssignStarosta_Mute_CheckHistory()
    {
        const string password = "StrongPassword123!";
        var curatorEmail = $"curator_{Guid.NewGuid():N}@kpi.ua";
        var studentEmail = $"student_{Guid.NewGuid():N}@kpi.ua";

        await _client.PostAsJsonAsync("/api/auth/register",
            new SignUpRequest(curatorEmail, password, "Олег", "Куратор", null));

        var curatorLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInRequest(curatorEmail, password));
        var curatorToken1 = (await curatorLogin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString()!;

        using var createGroupReq = new HttpRequestMessage(HttpMethod.Post, "/api/groups")
        {
            Content = JsonContent.Create(new CreateGroupRequest("ІП-44", "ФІОТ"))
        };
        createGroupReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", curatorToken1);
        var createGroupRes = await _client.SendAsync(createGroupReq);
        Assert.Equal(HttpStatusCode.Created, createGroupRes.StatusCode);

        var curatorReLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInRequest(curatorEmail, password));
        var curatorToken = (await curatorReLogin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString()!;

        using var inviteReq = new HttpRequestMessage(HttpMethod.Post, "/api/invitations")
        {
            Content = JsonContent.Create(new InviteUserRequest(studentEmail))
        };
        inviteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", curatorToken);
        var inviteRes = await _client.SendAsync(inviteReq);
        Assert.Equal(HttpStatusCode.Created, inviteRes.StatusCode);

        var studentReg = await _client.PostAsJsonAsync("/api/auth/register",
            new SignUpRequest(studentEmail, password, "Богдан", "Студент", null));
        Assert.Equal(HttpStatusCode.Created, studentReg.StatusCode);

        var studentLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInRequest(studentEmail, password));
        var studentDoc = await studentLogin.Content.ReadFromJsonAsync<JsonElement>();
        var studentToken = studentDoc.GetProperty("accessToken").GetString()!;
        var studentId = studentDoc.GetProperty("user").GetProperty("id").GetString()!;

        using var assignStarostaReq = new HttpRequestMessage(HttpMethod.Post, $"/api/members/{studentId}/assign-starosta");
        assignStarostaReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", curatorToken);
        var assignRes = await _client.SendAsync(assignStarostaReq);
        Assert.Equal(HttpStatusCode.OK, assignRes.StatusCode);

        var starostaLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInRequest(studentEmail, password));
        var starostaToken = (await starostaLogin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString()!;

        var thirdStudentEmail = $"third_{Guid.NewGuid():N}@kpi.ua";
        using var invite3Req = new HttpRequestMessage(HttpMethod.Post, "/api/invitations")
        {
            Content = JsonContent.Create(new InviteUserRequest(thirdStudentEmail))
        };
        invite3Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", curatorToken);
        await _client.SendAsync(invite3Req);

        var thirdReg = await _client.PostAsJsonAsync("/api/auth/register",
            new SignUpRequest(thirdStudentEmail, password, "Андрій", "Новачок", null));
        var thirdDoc = await thirdReg.Content.ReadFromJsonAsync<JsonElement>();
        var thirdId = thirdDoc.GetProperty("user").GetProperty("id").GetString()!;

        using var muteReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/members/{thirdId}/upload-access")
        {
            Content = JsonContent.Create(new SetUploadAccessRequest(false))
        };
        muteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", starostaToken);
        var muteRes = await _client.SendAsync(muteReq);
        Assert.Equal(HttpStatusCode.OK, muteRes.StatusCode);

        var mutedUser = await muteRes.Content.ReadFromJsonAsync<UserResponse>();
        Assert.False(mutedUser!.UploadAccess);

        using var historyReq = new HttpRequestMessage(HttpMethod.Get, "/api/history");
        historyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", curatorToken);
        var historyRes = await _client.SendAsync(historyReq);
        Assert.Equal(HttpStatusCode.OK, historyRes.StatusCode);

        var historyEvents = await historyRes.Content.ReadFromJsonAsync<List<HistoryEventResponse>>();
        Assert.NotNull(historyEvents);
        Assert.NotEmpty(historyEvents);

        Assert.Contains(historyEvents, e => e.Kind == "MemberAdded");
        Assert.Contains(historyEvents, e => e.Kind == "RoleChanged");
        Assert.Contains(historyEvents, e => e.Kind == "UploadAccess");
    }
}
