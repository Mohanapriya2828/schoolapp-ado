using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace SchoolApp.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly Dob { get; set; }

    public int? Age { get; set; }

    public string? Gender { get; set; }

    public string Designation { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phonenumber { get; set; }

    public string? Address { get; set; }

    public string Passwordhash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Profileimageurl { get; set; }

    public bool Isactive { get; set; }

    public DateTime Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public DateTime? Deletedat { get; set; }
    public void HashPassword()
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: this.Passwordhash,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        this.Passwordhash = $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    public bool VerifyPassword(string input)
    {
        var parts = this.Passwordhash.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var hash = parts[1];
        string hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: input,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        return hash == hashedInput;
    }

}
