using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketing.Data;
using StarEventsTicketing.Models;
using StarEventsTicketing.Services;
using StarEventsTicketing.ViewModels;

namespace StarEventsTicketing.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QRCodeService _qrCodeService;

        public BookingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            QRCodeService qrCodeService)
        {
            _context = context;
            _userManager = userManager;
            _qrCodeService = qrCodeService;
        }

        [HttpGet]
        public async Task<IActionResult> Book(int eventId, int numberOfTickets = 1)
        {
            var eventModel = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventModel == null || !eventModel.IsActive)
            {
                return NotFound();
            }

            if (eventModel.AvailableTickets < numberOfTickets)
            {
                ViewBag.ErrorMessage = "Not enough tickets available.";
                return RedirectToAction("Details", "Event", new { id = eventId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var totalAmount = eventModel.TicketPrice * numberOfTickets;
            var discountAmount = 0m;
            var finalAmount = totalAmount;

            ViewBag.Event = eventModel;
            ViewBag.NumberOfTickets = numberOfTickets;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.FinalAmount = finalAmount;
            ViewBag.LoyaltyPoints = user.LoyaltyPoints;
            ViewBag.Promotions = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookingViewModel model)
        {
            var eventModel = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == model.EventId);

            if (eventModel == null || !eventModel.IsActive)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (eventModel.AvailableTickets < model.NumberOfTickets)
            {
                ViewBag.ErrorMessage = "Not enough tickets available.";
                return RedirectToAction("Details", "Event", new { id = model.EventId });
            }

            var totalAmount = eventModel.TicketPrice * model.NumberOfTickets;
            var discountAmount = 0m;
            var loyaltyPointsUsed = 0;
            var loyaltyPointsEarned = 0;

            // Apply promotion code if provided
            if (!string.IsNullOrEmpty(model.PromotionCode))
            {
                var promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionCode == model.PromotionCode &&
                                             p.IsActive &&
                                             p.StartDate <= DateTime.Now &&
                                             p.EndDate >= DateTime.Now &&
                                             (p.MaxUses == null || p.CurrentUses < p.MaxUses));

                if (promotion != null)
                {
                    discountAmount = totalAmount * (promotion.DiscountPercentage / 100);
                    if (promotion.MaxDiscountAmount.HasValue && discountAmount > promotion.MaxDiscountAmount.Value)
                    {
                        discountAmount = promotion.MaxDiscountAmount.Value;
                    }
                    promotion.CurrentUses++;
                    _context.Update(promotion);
                }
            }

            // Apply loyalty points if requested
            if (model.UseLoyaltyPoints && user.LoyaltyPoints > 0)
            {
                var pointsValue = Math.Min(user.LoyaltyPoints, (int)(totalAmount - discountAmount) * 10);
                var discountFromPoints = pointsValue / 10m;
                discountAmount += discountFromPoints;
                loyaltyPointsUsed = pointsValue;
            }

            var finalAmount = Math.Max(0, totalAmount - discountAmount);

            // Create booking
            var booking = new Booking
            {
                UserId = user.Id,
                EventId = model.EventId,
                NumberOfTickets = model.NumberOfTickets,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                PaymentStatus = "Pending",
                BookingDate = DateTime.Now,
                LoyaltyPointsUsed = loyaltyPointsUsed,
                LoyaltyPointsEarned = loyaltyPointsEarned
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Process payment (simulated)
            booking.PaymentStatus = "Completed";
            booking.PaymentTransactionId = $"TXN{DateTime.Now:yyyyMMddHHmmss}{booking.BookingId}";
            booking.PaymentMethod = "Online Payment";

            // Update available tickets
            eventModel.AvailableTickets -= model.NumberOfTickets;
            _context.Update(eventModel);

            // Update loyalty points
            loyaltyPointsEarned = (int)(finalAmount / 10); // 1 point per 10 rupees
            user.LoyaltyPoints = user.LoyaltyPoints - loyaltyPointsUsed + loyaltyPointsEarned;
            booking.LoyaltyPointsEarned = loyaltyPointsEarned;
            await _userManager.UpdateAsync(user);

            // Generate tickets with QR codes
            for (int i = 0; i < model.NumberOfTickets; i++)
            {
                var ticketNumber = $"TKT{booking.BookingId:D6}{i + 1:D3}";
                var qrCodeData = $"{booking.BookingId}|{ticketNumber}|{eventModel.EventId}|{user.Id}";
                var qrCodeImagePath = await _qrCodeService.GenerateQRCodeAsync(qrCodeData, ticketNumber);

                var ticket = new Ticket
                {
                    BookingId = booking.BookingId,
                    TicketNumber = ticketNumber,
                    QRCode = qrCodeData,
                    QRCodeImagePath = qrCodeImagePath,
                    CreatedAt = DateTime.Now
                };

                _context.Tickets.Add(ticket);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("BookingConfirmation", new { id = booking.BookingId });
        }

        public async Task<IActionResult> BookingConfirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event!)
                .ThenInclude(e => e!.Venue)
                .Include(b => b.User)
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (booking.UserId != currentUser?.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(booking);
        }

        public async Task<IActionResult> MyBookings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e != null ? e.Venue : null)
                .Include(b => b.Tickets)
                .Where(b => b.UserId == currentUser.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> UpcomingEvents()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Where(b => b.UserId == currentUser.Id &&
                           b.PaymentStatus == "Completed" &&
                           b.Event != null &&
                           b.Event.EventDate >= DateTime.Today)
                .OrderBy(b => b.Event != null ? b.Event.EventDate : DateTime.MaxValue)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> DownloadTicket(int ticketId)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Booking!)
                .ThenInclude(b => b!.Event!)
                .ThenInclude(e => e!.Venue)
                .Include(t => t.Booking!)
                .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (ticket.Booking?.UserId != currentUser?.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(ticket);
        }
    }
}

