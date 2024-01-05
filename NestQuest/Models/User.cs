using NestQuest.Enum;
using NestQuest.Models;

public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string FirsName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public decimal? Classification { get; set; }
        public string? LongDescription { get; set; }
        public string? ShortDescription { get; set; }
        public string? Avatar { get; set; }
        public string UserType => Enum.GetName(typeof(UserRole), UserRole);
        public UserRole UserRole { get; set; }
        public ICollection<Hiring>? Hirings { get; set; }
        public DateTime? Birthdate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

