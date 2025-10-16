using System;
using System.Collections.Generic;

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
}
