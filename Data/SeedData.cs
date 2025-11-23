using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarEventsTicketing.Models;

namespace StarEventsTicketing.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            context.Database.EnsureCreated();

            // Create roles
            string[] roles = { "Admin", "Organizer", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create default Admin user
            if (!context.Users.Any(u => u.UserName == "admin@starevents.com"))
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@starevents.com",
                    Email = "admin@starevents.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed sample venues
            if (!context.Venues.Any())
            {
                context.Venues.AddRange(
                    new Venue
                    {
                        VenueName = "Nelum Pokuna Mahinda Rajapaksa Theatre",
                        Address = "Ananda Coomaraswamy Mawatha",
                        City = "Colombo",
                        PhoneNumber = "+94 11 234 5678",
                        Capacity = 1500,
                        Description = "Premier performing arts venue in Colombo",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Venue
                    {
                        VenueName = "Sugathadasa Stadium",
                        Address = "Sugathadasa Mawatha",
                        City = "Colombo",
                        PhoneNumber = "+94 11 234 5679",
                        Capacity = 25000,
                        Description = "Large stadium for concerts and events",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Venue
                    {
                        VenueName = "Lionel Wendt Theatre",
                        Address = "18 Guildford Crescent",
                        City = "Colombo",
                        PhoneNumber = "+94 11 234 5680",
                        Capacity = 500,
                        Description = "Intimate theatre for cultural performances",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Venue
                    {
                        VenueName = "Bandaranaike Memorial International Conference Hall",
                        Address = "Bauddhaloka Mawatha",
                        City = "Colombo",
                        PhoneNumber = "+94 11 269 4694",
                        Capacity = 1500,
                        Description = "International conference and event venue",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new Venue
                    {
                        VenueName = "Nelum Pokuna Fountainside",
                        Address = "Nelum Pokuna Mawatha",
                        City = "Colombo",
                        PhoneNumber = "+94 11 234 5681",
                        Capacity = 2000,
                        Description = "Outdoor event venue with fountain views",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    }
                );
                await context.SaveChangesAsync();
            }

            // Create sample Organizer users
            if (!context.Users.Any(u => u.Email == "organizer1@starevents.com"))
            {
                var organizer1 = new ApplicationUser
                {
                    UserName = "organizer1@starevents.com",
                    Email = "organizer1@starevents.com",
                    FirstName = "Kamal",
                    LastName = "Perera",
                    Role = "Organizer",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    City = "Colombo"
                };
                var result1 = await userManager.CreateAsync(organizer1, "Organizer@123");
                if (result1.Succeeded)
                {
                    await userManager.AddToRoleAsync(organizer1, "Organizer");
                }
            }

            if (!context.Users.Any(u => u.Email == "organizer2@starevents.com"))
            {
                var organizer2 = new ApplicationUser
                {
                    UserName = "organizer2@starevents.com",
                    Email = "organizer2@starevents.com",
                    FirstName = "Nimal",
                    LastName = "Fernando",
                    Role = "Organizer",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now.AddMonths(-4),
                    City = "Kandy"
                };
                var result2 = await userManager.CreateAsync(organizer2, "Organizer@123");
                if (result2.Succeeded)
                {
                    await userManager.AddToRoleAsync(organizer2, "Organizer");
                }
            }

            // Create sample Customer users
            if (!context.Users.Any(u => u.Email == "customer1@example.com"))
            {
                var customer1 = new ApplicationUser
                {
                    UserName = "customer1@example.com",
                    Email = "customer1@example.com",
                    FirstName = "Saman",
                    LastName = "Wijesinghe",
                    Role = "Customer",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    City = "Colombo",
                    LoyaltyPoints = 150
                };
                var result3 = await userManager.CreateAsync(customer1, "Customer@123");
                if (result3.Succeeded)
                {
                    await userManager.AddToRoleAsync(customer1, "Customer");
                }
            }

            if (!context.Users.Any(u => u.Email == "customer2@example.com"))
            {
                var customer2 = new ApplicationUser
                {
                    UserName = "customer2@example.com",
                    Email = "customer2@example.com",
                    FirstName = "Priya",
                    LastName = "Silva",
                    Role = "Customer",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    City = "Galle",
                    LoyaltyPoints = 50
                };
                var result4 = await userManager.CreateAsync(customer2, "Customer@123");
                if (result4.Succeeded)
                {
                    await userManager.AddToRoleAsync(customer2, "Customer");
                }
            }

            await context.SaveChangesAsync();

            // Seed sample events
            if (!context.Events.Any())
            {
                var venues = await context.Venues.ToListAsync();
                var organizers = await userManager.GetUsersInRoleAsync("Organizer");
                
                if (venues.Any() && organizers.Any())
                {
                    var organizer1 = organizers.FirstOrDefault();
                    var organizer2 = organizers.Count > 1 ? organizers[1] : organizer1;

                    if (organizer1 == null || !venues.Any())
                    {
                        // Skip event creation if no organizer or venues
                        return;
                    }

                    // Find venues with fallback to first venue if specific ones not found
                    var sugathadasaVenue = venues.FirstOrDefault(v => v.VenueName.Contains("Sugathadasa")) ?? venues.FirstOrDefault();
                    var lionelVenue = venues.FirstOrDefault(v => v.VenueName.Contains("Lionel")) ?? venues.FirstOrDefault();
                    var nelumVenue = venues.FirstOrDefault(v => v.VenueName.Contains("Nelum")) ?? venues.FirstOrDefault();
                    var bandaranaikeVenue = venues.FirstOrDefault(v => v.VenueName.Contains("Bandaranaike")) ?? venues.FirstOrDefault();

                    // Use first venue as default if any are still null
                    var defaultVenue = venues.FirstOrDefault();
                    if (defaultVenue == null) return;
                    
                    sugathadasaVenue ??= defaultVenue;
                    lionelVenue ??= defaultVenue;
                    nelumVenue ??= defaultVenue;
                    bandaranaikeVenue ??= defaultVenue;

                    context.Events.AddRange(
                        new Event
                        {
                            EventName = "Colombo Music Festival 2024",
                            Description = "A spectacular music festival featuring top local and international artists. Experience the best of contemporary and traditional music in one amazing event.",
                            EventDate = DateTime.Now.AddDays(30),
                            EventTime = new TimeSpan(18, 0, 0),
                            VenueId = sugathadasaVenue.VenueId,
                            Category = "Concert",
                            TicketPrice = 2500.00m,
                            TotalTickets = 20000,
                            AvailableTickets = 18500,
                            OrganizerId = organizer1!.Id,
                            IsActive = true,
                            ImageUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800",
                            CreatedAt = DateTime.Now.AddMonths(-2)
                        },
                        new Event
                        {
                            EventName = "Shakespeare's Hamlet - Theatre Production",
                            Description = "A classic production of Shakespeare's Hamlet performed by the National Theatre Company. Experience this timeless tragedy in an intimate setting.",
                            EventDate = DateTime.Now.AddDays(15),
                            EventTime = new TimeSpan(19, 30, 0),
                            VenueId = lionelVenue.VenueId,
                            Category = "Theatre",
                            TicketPrice = 1500.00m,
                            TotalTickets = 450,
                            AvailableTickets = 320,
                            OrganizerId = organizer1!.Id,
                            IsActive = true,
                            ImageUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?w=800",
                            CreatedAt = DateTime.Now.AddMonths(-1)
                        },
                        new Event
                        {
                            EventName = "Traditional Kandyan Dance Show",
                            Description = "Experience the rich cultural heritage of Sri Lanka through traditional Kandyan dance performances. Featuring renowned dancers and musicians.",
                            EventDate = DateTime.Now.AddDays(45),
                            EventTime = new TimeSpan(19, 0, 0),
                            VenueId = nelumVenue.VenueId,
                            Category = "Cultural",
                            TicketPrice = 1200.00m,
                            TotalTickets = 1400,
                            AvailableTickets = 1200,
                            OrganizerId = organizer2!.Id,
                            IsActive = true,
                            ImageUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=800",
                            CreatedAt = DateTime.Now.AddMonths(-1)
                        },
                        new Event
                        {
                            EventName = "Jazz Night with International Artists",
                            Description = "An evening of smooth jazz featuring international and local jazz musicians. Perfect for music lovers who enjoy sophisticated sounds.",
                            EventDate = DateTime.Now.AddDays(20),
                            EventTime = new TimeSpan(20, 0, 0),
                            VenueId = lionelVenue.VenueId,
                            Category = "Concert",
                            TicketPrice = 2000.00m,
                            TotalTickets = 480,
                            AvailableTickets = 450,
                            OrganizerId = organizer1!.Id,
                            IsActive = true,
                            ImageUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=800",
                            CreatedAt = DateTime.Now.AddDays(-20)
                        },
                        new Event
                        {
                            EventName = "Comedy Night - Stand Up Special",
                            Description = "Laugh your heart out with Sri Lanka's top comedians in this special stand-up comedy show. Guaranteed fun for the whole family.",
                            EventDate = DateTime.Now.AddDays(25),
                            EventTime = new TimeSpan(20, 30, 0),
                            VenueId = nelumVenue.VenueId,
                            Category = "Cultural",
                            TicketPrice = 1000.00m,
                            TotalTickets = 1500,
                            AvailableTickets = 1400,
                            OrganizerId = organizer2!.Id,
                            IsActive = true,
                            ImageUrl = "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?w=800",
                            CreatedAt = DateTime.Now.AddDays(-15)
                        },
                        new Event
                        {
                            EventName = "Classical Music Concert",
                            Description = "A beautiful evening of classical music featuring symphony orchestra and renowned soloists. Perfect for classical music enthusiasts.",
                            EventDate = DateTime.Now.AddDays(60),
                            EventTime = new TimeSpan(19, 0, 0),
                            VenueId = bandaranaikeVenue.VenueId,
                            Category = "Concert",
                            TicketPrice = 1800.00m,
                            TotalTickets = 1400,
                            AvailableTickets = 1400,
                            OrganizerId = organizer1!.Id,
                            IsActive = true,
                            ImageUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=800",
                            CreatedAt = DateTime.Now.AddDays(-10)
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }

            // Seed sample promotions
            if (!context.Promotions.Any())
            {
                context.Promotions.AddRange(
                    new Promotion
                    {
                        PromotionCode = "EARLYBIRD2024",
                        Description = "Early bird discount for early bookings",
                        DiscountPercentage = 15.00m,
                        MaxDiscountAmount = 500.00m,
                        StartDate = DateTime.Now.AddDays(-30),
                        EndDate = DateTime.Now.AddDays(60),
                        MaxUses = 1000,
                        CurrentUses = 45,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddMonths(-1)
                    },
                    new Promotion
                    {
                        PromotionCode = "STUDENT50",
                        Description = "Student discount - 50% off",
                        DiscountPercentage = 50.00m,
                        MaxDiscountAmount = 1000.00m,
                        StartDate = DateTime.Now.AddDays(-10),
                        EndDate = DateTime.Now.AddDays(90),
                        MaxUses = 500,
                        CurrentUses = 12,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-10)
                    },
                    new Promotion
                    {
                        PromotionCode = "WEEKEND10",
                        Description = "Weekend special discount",
                        DiscountPercentage = 10.00m,
                        MaxDiscountAmount = 300.00m,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(30),
                        MaxUses = null,
                        CurrentUses = 8,
                        IsActive = true,
                        CreatedAt = DateTime.Now.AddDays(-5)
                    }
                );
                await context.SaveChangesAsync();
            }

            // Seed sample bookings (if events and customers exist)
            if (!context.Bookings.Any())
            {
                var events = await context.Events.Where(e => e.IsActive).Take(3).ToListAsync();
                var customers = await userManager.GetUsersInRoleAsync("Customer");
                
                if (events.Any() && customers.Any())
                {
                    var customer1 = customers.FirstOrDefault();
                    var customer2 = customers.Count > 1 ? customers[1] : customer1;

                    var booking1 = new Booking
                    {
                        UserId = customer1!.Id,
                        EventId = events[0].EventId,
                        NumberOfTickets = 2,
                        TotalAmount = events[0].TicketPrice * 2,
                        DiscountAmount = 0,
                        FinalAmount = events[0].TicketPrice * 2,
                        PaymentStatus = "Completed",
                        PaymentTransactionId = "TXN20241122001",
                        PaymentMethod = "Online Payment",
                        BookingDate = DateTime.Now.AddDays(-10),
                        LoyaltyPointsUsed = 0,
                        LoyaltyPointsEarned = (int)((events[0].TicketPrice * 2) / 10)
                    };
                    context.Bookings.Add(booking1);

                    var booking2 = new Booking
                    {
                        UserId = customer1.Id,
                        EventId = events[1].EventId,
                        NumberOfTickets = 1,
                        TotalAmount = events[1].TicketPrice,
                        DiscountAmount = events[1].TicketPrice * 0.15m, // Early bird discount
                        FinalAmount = events[1].TicketPrice * 0.85m,
                        PaymentStatus = "Completed",
                        PaymentTransactionId = "TXN20241122002",
                        PaymentMethod = "Online Payment",
                        BookingDate = DateTime.Now.AddDays(-5),
                        LoyaltyPointsUsed = 0,
                        LoyaltyPointsEarned = (int)((events[1].TicketPrice * 0.85m) / 10)
                    };
                    context.Bookings.Add(booking2);

                    if (customer2 != null && events.Count > 2)
                    {
                        var booking3 = new Booking
                        {
                            UserId = customer2.Id,
                            EventId = events[2].EventId,
                            NumberOfTickets = 3,
                            TotalAmount = events[2].TicketPrice * 3,
                            DiscountAmount = 0,
                            FinalAmount = events[2].TicketPrice * 3,
                            PaymentStatus = "Completed",
                            PaymentTransactionId = "TXN20241122003",
                            PaymentMethod = "Online Payment",
                            BookingDate = DateTime.Now.AddDays(-3),
                            LoyaltyPointsUsed = 0,
                            LoyaltyPointsEarned = (int)((events[2].TicketPrice * 3) / 10)
                        };
                        context.Bookings.Add(booking3);
                    }

                    await context.SaveChangesAsync();

                    // Update event available tickets
                    events[0].AvailableTickets -= 2;
                    events[1].AvailableTickets -= 1;
                    if (events.Count > 2)
                    {
                        events[2].AvailableTickets -= 3;
                    }
                    context.Events.UpdateRange(events);
                    await context.SaveChangesAsync();

                    // Update customer loyalty points
                    customer1.LoyaltyPoints += booking1.LoyaltyPointsEarned + booking2.LoyaltyPointsEarned;
                    if (customer2 != null)
                    {
                        var booking3 = context.Bookings.FirstOrDefault(b => b.UserId == customer2.Id);
                        if (booking3 != null)
                        {
                            customer2.LoyaltyPoints += booking3.LoyaltyPointsEarned;
                            await userManager.UpdateAsync(customer2);
                        }
                    }
                    await userManager.UpdateAsync(customer1);
                }
            }
        }
    }
}

