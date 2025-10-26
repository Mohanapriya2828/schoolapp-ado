using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using SchoolApp.Controllers;
using SchoolApp.Models;

public interface IUserRepository
{
    int AddUser(User user);
    User? GetUserByEmail(string email);
    User? GetUserById(int id);
    List<User> GetAllUsers();
    bool UpdateUser(int id, UserUpdateRequest update);
    bool DeleteUser(int id);
}

public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;

    public UsersController(IConfiguration config, IUserRepository repo)
    {
        _config = config;
        _repo = repo;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] User user)
    {
        user.HashPassword();
        user.Createdat = DateTime.UtcNow;
        user.Isactive = true;

        int id = _repo.AddUser(user);
        user.Id = id;
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _repo.GetUserByEmail(request.Email);
        if (user == null || !user.VerifyPassword(request.Password))
            return Unauthorized("Invalid credentials");
        return Ok(new { id = user.Id, token = "fake-jwt-token", role = user.Role, name = user.Name });
    }

    [HttpGet]
    public IActionResult GetUsers()
    {
        var users = _repo.GetAllUsers();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var user = _repo.GetUserById(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateUser(int id, UserUpdateRequest update)
    {
        bool updated = _repo.UpdateUser(id, update);
        if (!updated) return NotFound(new { message = "User not found" });
        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteUser(int id)
    {
        bool deleted = _repo.DeleteUser(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

public class UsersControllerUnitTests
{
    private readonly UsersController _controller;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IUserRepository> _mockRepo;

    public UsersControllerUnitTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(a => a["Jwt:Key"]).Returns("test_secret_key_1234567890");
        _mockConfig.Setup(a => a["Jwt:Issuer"]).Returns("test_issuer");
        _mockConfig.Setup(a => a["Jwt:Audience"]).Returns("test_audience");
        _mockConfig.Setup(a => a["Jwt:ExpiryMinutes"]).Returns("30");

        _mockRepo = new Mock<IUserRepository>();

        _controller = new UsersController(_mockConfig.Object, _mockRepo.Object);
    }

    [Fact]
    public void Register_ValidUser_ReturnsCreatedAtAction()
    {
        var user = new User { Id = 0, Name = "Test User", Email = "testuser@example.com", Passwordhash = "hash", Role = "Teacher" };

        _mockRepo.Setup(r => r.AddUser(It.IsAny<User>())).Returns(1);

        var result = _controller.Register(user);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnUser = Assert.IsType<User>(createdResult.Value);
        Assert.Equal(1, returnUser.Id);
    }

    [Fact]
    public void Login_InvalidCredentials_ReturnsUnauthorized()
    {
        _mockRepo.Setup(r => r.GetUserByEmail(It.IsAny<string>())).Returns<User?>(null);

        var loginRequest = new LoginRequest { Email = "nonexistent@example.com", Password = "wrongpass" };
        var result = _controller.Login(loginRequest);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void GetUser_NonExisting_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.GetUserById(It.IsAny<int>())).Returns<User?>(null);

        var result = _controller.GetUser(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetUsers_ReturnsOkWithUsers()
    {
        var users = new List<User> { new User { Id = 1, Name = "User One" }, new User { Id = 2, Name = "User Two" } };
        _mockRepo.Setup(r => r.GetAllUsers()).Returns(users);

        var result = _controller.GetUsers();
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnUsers = Assert.IsAssignableFrom<IEnumerable<User>>(okResult.Value);

        Assert.Equal(2, ((List<User>)returnUsers).Count);
    }

    [Fact]
    public void UpdateUser_NonExisting_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.UpdateUser(It.IsAny<int>(), It.IsAny<UserUpdateRequest>())).Returns(false);

        var update = new UserUpdateRequest { Name = "Updated Name", Email = "updated@example.com" };
        var result = _controller.UpdateUser(9999, update);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteUser_Existing_ReturnsNoContent()
    {
        _mockRepo.Setup(r => r.DeleteUser(It.IsAny<int>())).Returns(true);

        var result = _controller.DeleteUser(1);
        Assert.IsType<NoContentResult>(result);
    }
}
