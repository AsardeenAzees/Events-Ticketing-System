using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketing.Data;
using StarEventsTicketing.Models;

namespace StarEventsTicketing.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? category, string? city, DateTime? date)
        {
            var query = _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Where(e => e.IsActive && e.EventDate >= DateTime.Today);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(e => e.Venue != null && e.Venue.City == city);
            }

            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate.Date == date.Value.Date);
            }

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();

            ViewBag.Categories = await _context.Events
                .Select(e => e.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Cities = await _context.Venues
                .Select(v => v.City)
                .Distinct()
                .ToListAsync();

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedDate = date;

            return View(events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var eventModel = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventModel == null)
            {
                return NotFound();
            }

            return View(eventModel);
        }

        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> MyEvents()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // For Organizer: Show events where they are the organizer
            // This includes events created by them AND events assigned to them by Admin
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Where(e => e.OrganizerId == currentUser.Id)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            // Get admin user IDs to determine if event was created by admin
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserIds = adminUsers.Select(u => u.Id).ToHashSet();
            
            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.AdminUserIds = adminUserIds;

            return View(events);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            return View();
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event eventModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Set organizer ID BEFORE validation - for Organizer role, use current user
            if (!User.IsInRole("Admin"))
            {
                eventModel.OrganizerId = currentUser.Id;
                // Remove OrganizerId validation error if it exists, since we're setting it programmatically
                ModelState.Remove("OrganizerId");
            }
            // If Admin, OrganizerId should be set from the form (if provided)

            // Ensure IsActive is set (defaults to true)
            eventModel.IsActive = true;

            if (ModelState.IsValid)
            {
                try
                {
                    eventModel.CreatedAt = DateTime.Now;
                    eventModel.AvailableTickets = eventModel.TotalTickets;
                    
                    _context.Events.Add(eventModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Event created successfully!";
                    return RedirectToAction(nameof(MyEvents));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating event: {ex.Message}");
                }
            }

            // If we get here, there were validation errors
            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            return View(eventModel);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            // Check if user is the organizer or admin
            if (eventModel.OrganizerId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            return View(eventModel);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event eventModel)
        {
            if (id != eventModel.EventId)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound();
            }

            // Check if user is the organizer or admin
            if (existingEvent.OrganizerId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
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
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(eventModel.EventId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(MyEvents));
            }

            ViewBag.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            return View(eventModel);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                return NotFound();
            }

            // Check if user is the organizer or admin
            if (eventModel.OrganizerId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Check if there are any bookings for this event
            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Cannot delete event with existing bookings. Please deactivate it instead.";
                return RedirectToAction(nameof(MyEvents));
            }

            _context.Events.Remove(eventModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event deleted successfully.";
            return RedirectToAction(nameof(MyEvents));
        }

        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> SalesReport(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var eventModel = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventModel == null)
            {
                return NotFound();
            }

            // Check if user is the organizer or admin
            if (eventModel.OrganizerId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.EventId == id && b.PaymentStatus == "Completed")
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var totalRevenue = bookings.Sum(b => b.FinalAmount);
            var totalTicketsSold = bookings.Sum(b => b.NumberOfTickets);

            ViewBag.Event = eventModel;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalTicketsSold = totalTicketsSold;
            ViewBag.Bookings = bookings;

            return View();
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}

