using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SchoolApp.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SchoolApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public UsersController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            user.HashPassword();
            user.Createdat = DateTime.UtcNow;
            user.Isactive = true;

            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"INSERT INTO Users (Name, DOB, Age, Department, Designation, Email, PhoneNumber, Address, PasswordHash, Role, IsActive, CreatedAt)
                              VALUES (@Name,@DOB,@Age,@Department,@Designation,@Email,@PhoneNumber,@Address,@PasswordHash,@Role,@IsActive,@CreatedAt);
                              SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@DOB", (object?)user.Dob ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Age", (object?)user.Age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Department", (object?)user.Department ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Designation", (object?)user.Designation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", (object?)user.Phonenumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)user.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PasswordHash", user.Passwordhash);
                    cmd.Parameters.AddWithValue("@Role", user.Role);
                    cmd.Parameters.AddWithValue("@IsActive", user.Isactive);
                    cmd.Parameters.AddWithValue("@CreatedAt", user.Createdat);

                    conn.Open();
                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    user.Id = id;
                }
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            User? user = null;

            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Users WHERE Email=@Email";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Role = reader["Role"].ToString(),
                                Passwordhash = reader["PasswordHash"].ToString(),
                                Dob = reader["Dob"] != DBNull.Value
                                ? DateOnly.FromDateTime((DateTime)reader["Dob"])
                                : default,
                                Age = reader["Age"] != DBNull.Value ? (int)reader["Age"] : 0,
                                Department = reader["Department"].ToString(),
                                Designation = reader["Designation"].ToString(),
                                Phonenumber = reader["PhoneNumber"].ToString(),
                                Address = reader["Address"].ToString(),
                                Isactive = reader["IsActive"] != DBNull.Value ? (bool)reader["IsActive"] : true
                            };
                        }
                    }
                }
            }

            if (user == null || !user.VerifyPassword(request.Password))
                return Unauthorized("Invalid credentials");

            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Name, user.Name)
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"])),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { id = user.Id, token = tokenHandler.WriteToken(token), role = user.Role, name = user.Name });
        }

        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public IActionResult GetUsers()
        {
            var users = new List<User>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Users WHERE IsActive = 1";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Role = reader["Role"].ToString(),
                                Dob = reader["Dob"] != DBNull.Value
                                ? DateOnly.FromDateTime((DateTime)reader["Dob"])
                                : default,

                                Age = reader["Age"] != DBNull.Value ? (int)reader["Age"] : 0,
                                Department = reader["Department"].ToString(),
                                Designation = reader["Designation"].ToString(),
                                Phonenumber = reader["PhoneNumber"].ToString(),
                                Address = reader["Address"].ToString(),
                                Isactive = reader["IsActive"] != DBNull.Value ? (bool)reader["IsActive"] : true
                            });
                        }
                    }
                }
            }
            return Ok(users);
        }


        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetUser(int id)
        {
            User? user = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Users WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Role = reader["Role"].ToString(),
                                Dob = reader["Dob"] != DBNull.Value
                                ? DateOnly.FromDateTime((DateTime)reader["Dob"])
                                : default,

                                Age = reader["Age"] != DBNull.Value ? (int)reader["Age"] : 0,
                                Department = reader["Department"].ToString(),
                                Designation = reader["Designation"].ToString(),
                                Phonenumber = reader["PhoneNumber"].ToString(),
                                Address = reader["Address"].ToString(),
                                Profileimageurl = reader["ProfileImageUrl"]?.ToString(),
                                Isactive = reader["IsActive"] != DBNull.Value ? (bool)reader["IsActive"] : true
                            };
                        }
                    }
                }
            }

            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher")]
        public IActionResult UpdateUser(int id, [FromBody] UserUpdateRequest update)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                User? existingUser = null;
                var selectQuery = "SELECT * FROM Users WHERE Id = @Id AND IsActive = 1";
                using (var selectCmd = new SqlCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingUser = new User
                            {
                                Id = (int)reader["Id"],
                                Role = reader["Role"].ToString(),
                                Email = reader["Email"].ToString()
                            };
                        }
                    }
                }
                if (existingUser == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var emailCheckQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND Id != @Id AND IsActive = 1";
                using (var emailCmd = new SqlCommand(emailCheckQuery, conn))
                {
                    emailCmd.Parameters.AddWithValue("@Email", update.Email ?? (object)DBNull.Value);
                    emailCmd.Parameters.AddWithValue("@Id", id);
                    int count = (int)emailCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        return BadRequest(new { message = "Email already exists for another user" });
                    }
                }
                var updateQuery = @"UPDATE Users 
                            SET Name = @Name, 
                                Email = @Email, 
                                Dob = @Dob,
                                Gender = @Gender,
                                Department = @Department, 
                                Designation = @Designation, 
                                PhoneNumber = @PhoneNumber, 
                                Address = @Address,
                                ProfileImageUrl = @ProfileImageUrl,
                                UpdatedAt = @UpdatedAt" +
                                        (string.IsNullOrEmpty(update.PasswordHash)
                                            ? ""
                                            : ", PasswordHash = @PasswordHash") +
                                        " WHERE Id = @Id";

                using (var updateCmd = new SqlCommand(updateQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@Id", id);
                    updateCmd.Parameters.AddWithValue("@Name", (object?)update.Name ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Email", (object?)update.Email ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Dob", update.Dob.HasValue ? update.Dob.Value.ToDateTime(TimeOnly.MinValue) : (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Gender", (object?)update.Gender ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Department", (object?)update.Department ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Designation", (object?)update.Designation ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@PhoneNumber", (object?)update.Phonenumber ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Address", (object?)update.Address ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@ProfileImageUrl", (object?)update.ProfileImageUrl ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    if (!string.IsNullOrEmpty(update.PasswordHash))
                    {
                        updateCmd.Parameters.AddWithValue("@PasswordHash", update.PasswordHash);
                    }

                    int rowsAffected = updateCmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = "Update failed" });
                    }
                }
            }

            return Ok(new { message = "User updated successfully" });
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Teacher")]
        public IActionResult DeleteUser(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"UPDATE Users SET IsActive=0, DeletedAt=@DeletedAt WHERE Id=@Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@DeletedAt", DateTime.UtcNow);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return NoContent();
        }
    }
}
