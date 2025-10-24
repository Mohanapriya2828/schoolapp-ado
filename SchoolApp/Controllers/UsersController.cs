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
                    cmd.Parameters.AddWithValue("@Age", user.Age);
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
            return Ok(new { token = tokenHandler.WriteToken(token), role = user.Role, name = user.Name });
        }

        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public IActionResult GetUsers()
        {
            var users = new List<User>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Users";
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
        public IActionResult PutUser(int id, [FromBody] User updatedUser)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var query = @"UPDATE Users SET 
                              Name=@Name, DOB=@DOB, Age=@Age, Department=@Department, Designation=@Designation,
                              Email=@Email, PhoneNumber=@PhoneNumber, Address=@Address, PasswordHash=@PasswordHash, Role=@Role
                              WHERE Id=@Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", (object?)updatedUser.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DOB", (object?)updatedUser.Dob ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Age", updatedUser.Age);
                    cmd.Parameters.AddWithValue("@Department", (object?)updatedUser.Department ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Designation", (object?)updatedUser.Designation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)updatedUser.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNumber", (object?)updatedUser.Phonenumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)updatedUser.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PasswordHash", (object?)updatedUser.Passwordhash ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", updatedUser.Role);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return NoContent();
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
