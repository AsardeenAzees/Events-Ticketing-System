using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEventsTicketing.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [Required]
        [StringLength(100)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string QRCode { get; set; } = string.Empty;

        [StringLength(200)]
        public string? QRCodeImagePath { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

