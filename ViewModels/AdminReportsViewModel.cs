using StarEventsTicketing.Models;

namespace StarEventsTicketing.ViewModels
{
    public class AdminReportsViewModel
    {
        public List<Booking> SalesReport { get; set; } = new List<Booking>();
        public List<Event> EventsReport { get; set; } = new List<Event>();
        public List<ApplicationUser> UsersReport { get; set; } = new List<ApplicationUser>();
    }
}

