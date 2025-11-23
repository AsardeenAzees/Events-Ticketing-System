using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketing.Data;
using StarEventsTicketing.Models;
using StarEventsTicketing.ViewModels;

namespace StarEventsTicketing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var totalEvents = await _context.Events.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                var totalBookings = await _context.Bookings.CountAsync();
                
                // Safely calculate revenue - handle empty collections
                decimal totalRevenue = 0m;
                var completedBookings = await _context.Bookings
                    .Where(b => b.PaymentStatus == "Completed")
                    .ToListAsync();
                
                if (completedBookings.Any())
                {
                    totalRevenue = completedBookings.Sum(b => b.FinalAmount);
                }

                var recentBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Event)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync();

                var model = new AdminDashboardViewModel
                {
                    TotalEvents = totalEvents,
                    TotalUsers = totalUsers,
                    TotalBookings = totalBookings,
                    TotalRevenue = totalRevenue,
                    RecentBookings = recentBookings ?? new List<Booking>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // Log error and return view with default values
                var model = new AdminDashboardViewModel
                {
                    TotalEvents = 0,
                    TotalUsers = 0,
                    TotalBookings = 0,
                    TotalRevenue = 0m,
                    RecentBookings = new List<Booking>()
                };
                return View(model);
            }
        }

        // Venue Management
        public async Task<IActionResult> Venues()
        {
            var venues = await _context.Venues.OrderBy(v => v.VenueName).ToListAsync();
            return View(venues);
        }

        [HttpGet]
        public IActionResult CreateVenue()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(Venue venue)
        {
            if (ModelState.IsValid)
            {
                venue.CreatedAt = DateTime.Now;
                _context.Venues.Add(venue);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Venues));
            }
            return View(venue);
        }

        [HttpGet]
        public async Task<IActionResult> EditVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(int id, Venue venue)
        {
            if (id != venue.VenueId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.VenueId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Venues));
            }
            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue != null)
            {
                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Venues));
        }

        // Event Management
        public async Task<IActionResult> Events()
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            
            // Get Admin user IDs
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserIds = adminUsers.Select(u => u.Id).ToHashSet();
            
            // Determine which events were created by Admin
            // Since Admin can create events and assign them to Organizers,
            // we need to check: if OrganizerId is an Admin, OR if the event was created through Admin panel
            // For now, we'll check if OrganizerId belongs to an Admin user
            var eventCreatorInfo = new Dictionary<int, bool>();
            foreach (var evt in events)
            {
                if (string.IsNullOrEmpty(evt.OrganizerId))
                {
                    eventCreatorInfo[evt.EventId] = false;
                    continue;
                }
                
                // Check if OrganizerId is an Admin user ID
                var isAdminCreated = adminUserIds.Contains(evt.OrganizerId);
                
                // If not found by ID, check if the organizer user has Admin role
                if (!isAdminCreated && evt.Organizer != null)
                {
                    try
                    {
                        var organizerRoles = await _userManager.GetRolesAsync(evt.Organizer);
                        isAdminCreated = organizerRoles.Contains("Admin");
                    }
                    catch
                    {
                        isAdminCreated = false;
                    }
                }
                
                eventCreatorInfo[evt.EventId] = isAdminCreated;
            }
            
            ViewBag.AdminUserIds = adminUserIds;
            ViewBag.EventCreatorInfo = eventCreatorInfo;
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {
            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            ViewBag.Organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            ViewBag.AdminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event eventModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // If Admin creates event but doesn't assign OrganizerId, set it to Admin
            if (string.IsNullOrEmpty(eventModel.OrganizerId))
            {
                eventModel.OrganizerId = currentUser.Id;
            }

            if (ModelState.IsValid)
            {
                eventModel.CreatedAt = DateTime.Now;
                eventModel.AvailableTickets = eventModel.TotalTickets;
                
                _context.Events.Add(eventModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(Events));
            }

            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            ViewBag.Organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            ViewBag.AdminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return View(eventModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            ViewBag.Organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            return View(eventModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event eventModel)
        {
            if (id != eventModel.EventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEvent = await _context.Events.FindAsync(id);
                    if (existingEvent != null)
                    {
                        existingEvent.EventName = eventModel.EventName;
                        existingEvent.Description = eventModel.Description;
                        existingEvent.EventDate = eventModel.EventDate;
                        existingEvent.EventTime = eventModel.EventTime;
                        existingEvent.VenueId = eventModel.VenueId;
                        existingEvent.Category = eventModel.Category;
                        existingEvent.TicketPrice = eventModel.TicketPrice;
                        existingEvent.ImageUrl = eventModel.ImageUrl;
                        existingEvent.IsActive = eventModel.IsActive;
                        existingEvent.OrganizerId = eventModel.OrganizerId;

                        // Update available tickets if total tickets changed
                        if (eventModel.TotalTickets != existingEvent.TotalTickets)
                        {
                            var bookedTickets = await _context.Bookings
                                .Where(b => b.EventId == id && b.PaymentStatus == "Completed")
                                .SumAsync(b => b.NumberOfTickets);
                            existingEvent.TotalTickets = eventModel.TotalTickets;
                            existingEvent.AvailableTickets = eventModel.TotalTickets - bookedTickets;
                        }

                        _context.Update(existingEvent);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(eventModel.EventId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Events));
            }

            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            ViewBag.Organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            return View(eventModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel != null)
            {
                _context.Events.Remove(eventModel);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Events));
        }

        // User Management
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.LockoutEnabled = !user.LockoutEnabled;
                if (user.LockoutEnabled)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                }
                else
                {
                    user.LockoutEnd = null;
                }
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction(nameof(Users));
        }

        // Reports
        public async Task<IActionResult> Reports()
        {
            var salesReport = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.User)
                .Where(b => b.PaymentStatus == "Completed")
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var eventsReport = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .ToListAsync();

            var usersReport = await _context.Users.ToListAsync();

            var model = new AdminReportsViewModel
            {
                SalesReport = salesReport,
                EventsReport = eventsReport,
                UsersReport = usersReport
            };

            return View(model);
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.VenueId == id);
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}

