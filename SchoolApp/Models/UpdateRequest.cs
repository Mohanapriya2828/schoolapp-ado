namespace SchoolApp.Models
{
        public class UserUpdateRequest
        {
            public string Name { get; set; } = null!;
            public DateOnly? Dob { get; set; }      
            public string Gender { get; set; }
            public string Designation { get; set; }
            public string Department { get; set; }
            public string Email { get; set; } = null!;
            public string Phonenumber { get; set; }
            public string Address { get; set; }
            public string ProfileImageUrl { get; set; } = string.Empty;
            public string? PasswordHash { get; set; }
        }
  

}
