using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using SchoolApp.Models;
using System.Threading.Tasks;
using System;

public class UsersControllerSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetJwtTokenAsync()
    {
        var loginRequest = new { Email = "unittestuser@example.com", Password = "UnitTest@123" };
        var response = await _client.PostAsJsonAsync("api/users/login", loginRequest);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Register_ShouldSucceed()
    {
        var user = new User
        {
            Name = "Integration Smoke Test",
            Dob = DateOnly.Parse("1990-01-01"),
            Age = 33,
            Department = "Testing",
            Designation = "Engineer",
            Email = $"smoke_{Guid.NewGuid()}@example.com",
            Phonenumber = "9998887770",
            Address = "Smoke Address",
            Passwordhash = "Smoke@123",
            Role = "Teacher",
            Isactive = true
        };

        var response = await _client.PostAsJsonAsync("api/users/register", user);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Login_ShouldSucceed()
    {
        var loginRequest = new { Email = "unittestuser@example.com", Password = "UnitTest@123" };
        var response = await _client.PostAsJsonAsync("api/users/login", loginRequest);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnSuccess_WithAuth()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("api/users");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WithAuth()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var update = new UserUpdateRequest
        {
            Name = "Updated Smoke Test",
            Email = $"smoketest_{Guid.NewGuid()}@example.com",
            Gender = "Female",
            Designation = "QA Lead",
            Department = "Testing",
            Phonenumber = "7776665554",
            Address = "Updated Smoke Location",
            ProfileImageUrl = "http://example.com/test.png"
        };

        var response = await _client.PutAsJsonAsync("api/users/1", update);
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WithAuth()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.DeleteAsync("api/users/1");
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }
}
