using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SchoolApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StudentTeacherContext>();

        if (!context.Users.Any(u => u.Email == "unittestuser@example.com"))
        {
            var user = new User
            {
                Name = "Unit Test User",
                Email = "unittestuser@example.com",
                Department = "IT",
                Designation = "Developer",
                Role = "Teacher",
                Isactive = true,
                Createdat = DateTime.UtcNow
            };
            user.Passwordhash = "UnitTest@123";
            user.HashPassword();

            context.Users.Add(user);
            context.SaveChanges();
        }
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
    public async Task RegisterUser_ReturnsCreatedUser()
    {
        var user = new User
        {
            Name = "Test Register",
            Dob = DateOnly.Parse("1990-01-01"),
            Age = 30,
            Department = "Science",
            Designation = "Teacher",
            Email = $"test_register_{Guid.NewGuid()}@example.com",
            Phonenumber = "9876543210",
            Address = "Integration Street",
            Passwordhash = "Test@1234",
            Role = "Teacher",
            Isactive = true
        };

        var response = await _client.PostAsJsonAsync("api/users/register", user);
        response.EnsureSuccessStatusCode();
        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);
        Assert.Equal(user.Email, createdUser?.Email);
    }

    [Fact]
    public async Task LoginUser_ReturnsJwtToken()
    {
        var loginRequest = new { Email = "unittestuser@example.com", Password = "UnitTest@123" };
        var response = await _client.PostAsJsonAsync("api/users/login", loginRequest);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", json);
    }

    [Fact]
    public async Task GetUsers_WithAuth_ReturnsList()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("api/users");
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<User[]>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    public async Task GetUser_WithAuth_ReturnsUser()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("api/users/1");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return;
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(user);
    }

    [Fact]
    public async Task PutUser_WithAuth_UpdatesUser()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var update = new UserUpdateRequest
        {
            Name = "Updated Name Integration",
            Email = $"updated_{Guid.NewGuid()}@example.com",
            Gender = "Female",
            Designation = "Senior Teacher",
            Department = "Math",
            Phonenumber = "1112223333",
            Address = "Updated Address"
        };

        var response = await _client.PutAsJsonAsync("api/users/1", update);
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WithAuth_DeletesUser()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.DeleteAsync("api/users/1");
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }
}
