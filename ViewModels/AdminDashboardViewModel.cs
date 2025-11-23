using StarEventsTicketing.Models;

namespace StarEventsTicketing.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Booking> RecentBookings { get; set; } = new List<Booking>();
    }
}

