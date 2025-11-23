using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StarEventsTicketing.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        // PhoneNumber is inherited from IdentityUser, no need to redeclare

        public DateTime DateOfBirth { get; set; }

        public int LoyaltyPoints { get; set; } = 0;

        public string Role { get; set; } = "Customer"; // Customer, Organizer, Admin

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}

