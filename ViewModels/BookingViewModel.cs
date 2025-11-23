using System.ComponentModel.DataAnnotations;

namespace StarEventsTicketing.ViewModels
{
    public class BookingViewModel
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Number of tickets must be between 1 and 10")]
        [Display(Name = "Number of Tickets")]
        public int NumberOfTickets { get; set; } = 1;

        [Display(Name = "Promotion Code")]
        public string? PromotionCode { get; set; }

        [Display(Name = "Use Loyalty Points")]
        public bool UseLoyaltyPoints { get; set; }
    }
}

