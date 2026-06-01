using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Models;
using BookingService.Validators;
using System.Text.Json;

namespace BookingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BookingController(BookingDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("Create")]
        public async Task<ActionResult<Booking>> Create([FromBody] BookingRequest request)
        {
            try
            {
                Console.WriteLine("=== BOOKING CREATE START ===");
                Console.WriteLine($"Request received: {System.Text.Json.JsonSerializer.Serialize(request)}");

                // ── Validate request ──────────────────────────────────
                if (request == null)
                {
                    Console.WriteLine("ERROR: Request is null");
                    return BadRequest(new { success = false, message = "Request body is empty" });
                }

                var validationErrors = BookingValidator.Validate(request);
                if (validationErrors.Any())
                {
                    return BadRequest(new { success = false, errors = validationErrors });
                }

                // Get Tutor Service URL from config
                var tutorServiceUrl = _configuration["ServiceUrls:TutorService"] ?? "https://localhost:7228";
                Console.WriteLine($"Using TutorService URL: {tutorServiceUrl}");

                var tutorClient = _httpClientFactory.CreateClient();
                tutorClient.BaseAddress = new Uri(tutorServiceUrl);

                Console.WriteLine($"Calling: api/Tutor/GetById/{request.TutorID}");
                var tutorResponse = await tutorClient.GetAsync($"api/Tutor/GetById/{request.TutorID}");

                if (!tutorResponse.IsSuccessStatusCode)
                {
                    var error = await tutorResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"TutorService Error: {tutorResponse.StatusCode} - {error}");
                    return BadRequest(new { message = $"Tutor not found: {request.TutorID}" });
                }

                var tutorJson = await tutorResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Tutor JSON: {tutorJson}");

                using var tutorDoc = JsonDocument.Parse(tutorJson);

                // Get tutor status
                string tutorStatus = "";
                if (tutorDoc.RootElement.TryGetProperty("status", out var statusProp))
                    tutorStatus = statusProp.GetString();
                else if (tutorDoc.RootElement.TryGetProperty("Status", out var statusProp2))
                    tutorStatus = statusProp2.GetString();

                Console.WriteLine($"Tutor Status: {tutorStatus}");

                if (tutorStatus != "Approved")
                {
                    return BadRequest(new { message = "Tutor is not approved yet" });
                }

                // Get hourly rate
                decimal hourlyRate = 0;
                if (tutorDoc.RootElement.TryGetProperty("hourlyRate", out var rateProp))
                    hourlyRate = rateProp.GetDecimal();
                else if (tutorDoc.RootElement.TryGetProperty("HourlyRate", out var rateProp2))
                    hourlyRate = rateProp2.GetDecimal();

                Console.WriteLine($"Hourly Rate: {hourlyRate}");

                var totalAmount = hourlyRate * request.DurationHours;
                Console.WriteLine($"Total Amount: {totalAmount}");

                // Create booking
                // Create booking - don't set SlotID if it's empty
                var booking = new Booking
                {
                    BookingID = Guid.NewGuid().ToString(),
                    StudentID = request.StudentID,
                    TutorID = request.TutorID,
                    SlotID = string.IsNullOrEmpty(request.SlotID) ? null : request.SlotID,  // ✅ Fix here
                    BookingDate = request.BookingDate,
                    DurationHours = request.DurationHours,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    Topic = request.Topic,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"Adding booking to database...");
                _context.Bookings.Add(booking);

                Console.WriteLine($"Saving changes...");
                await _context.SaveChangesAsync();

                Console.WriteLine($"Booking created successfully with ID: {booking.BookingID}");

                return Ok(booking);
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DATABASE ERROR: {dbEx.Message}");
                Console.WriteLine($"Inner: {dbEx.InnerException?.Message}");
                return StatusCode(500, new { message = "Database error", details = dbEx.InnerException?.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GENERAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        // GET: api/Booking/GetAll
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetAll()
        {
            return await _context.Bookings.ToListAsync();
        }

        // GET: api/Booking/GetById/{id}
        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<Booking>> GetById(string id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();
            return booking;
        }

        // GET: api/Booking/GetByStudent/{studentId}
        [HttpGet("GetByStudent/{studentId}")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetByStudent(string studentId)
        {
            return await _context.Bookings.Where(b => b.StudentID == studentId).ToListAsync();
        }

        // GET: api/Booking/GetByTutor/{tutorId}
        [HttpGet("GetByTutor/{tutorId}")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetByTutor(string tutorId)
        {
            return await _context.Bookings.Where(b => b.TutorID == tutorId).ToListAsync();
        }

        // GET: api/Booking/GetBookingsWithDetails
        [HttpGet("GetBookingsWithDetails")]
        public async Task<ActionResult<IEnumerable<BookingWithDetails>>> GetBookingsWithDetails([FromQuery] string? studentId, [FromQuery] string? tutorId)
        {
            var userServiceUrl = _configuration["ServiceUrls:UserService"] ?? "https://localhost:7292";
            var tutorServiceUrl = _configuration["ServiceUrls:TutorService"] ?? "https://localhost:7228";

            var query = _context.Bookings.AsQueryable();

            if (!string.IsNullOrEmpty(studentId))
                query = query.Where(b => b.StudentID == studentId);
            if (!string.IsNullOrEmpty(tutorId))
                query = query.Where(b => b.TutorID == tutorId);

            var bookings = await query.ToListAsync();
            var result = new List<BookingWithDetails>();

            var userClient = _httpClientFactory.CreateClient();
            userClient.BaseAddress = new Uri(userServiceUrl);

            var tutorClient = _httpClientFactory.CreateClient();
            tutorClient.BaseAddress = new Uri(tutorServiceUrl);

            foreach (var booking in bookings)
            {
                // ── FIX: StudentID in Bookings != UserID in Users table.
                // We must first resolve StudentID → UserID via UserService GetStudentById,
                // then look up the user's FullName via GetById.
                string studentName = "Unknown";
                try
                {
                    var studentProfileResp = await userClient.GetAsync($"api/User/GetStudentById/{booking.StudentID}");
                    if (studentProfileResp.IsSuccessStatusCode)
                    {
                        var spJson = await studentProfileResp.Content.ReadAsStringAsync();
                        using var spDoc = JsonDocument.Parse(spJson);
                        if (spDoc.RootElement.TryGetProperty("userID", out var uidProp)
                            || spDoc.RootElement.TryGetProperty("UserID", out uidProp))
                        {
                            var resolvedUserId = uidProp.GetString();
                            if (!string.IsNullOrEmpty(resolvedUserId))
                            {
                                var userResp = await userClient.GetAsync($"api/User/GetById/{resolvedUserId}");
                                if (userResp.IsSuccessStatusCode)
                                {
                                    var userJson = await userResp.Content.ReadAsStringAsync();
                                    using var userDoc = JsonDocument.Parse(userJson);
                                    if (userDoc.RootElement.TryGetProperty("fullName", out var fn)
                                        || userDoc.RootElement.TryGetProperty("FullName", out fn))
                                        studentName = fn.GetString() ?? "Unknown";
                                }
                            }
                        }
                    }
                }
                catch { /* keep "Unknown" on any error */ }

                // Get tutor name and specialization
                var tutorResponse = await tutorClient.GetAsync($"api/Tutor/GetById/{booking.TutorID}");
                string tutorName = "Unknown";
                string tutorSpecialization = "";
                if (tutorResponse.IsSuccessStatusCode)
                {
                    var tutorJson = await tutorResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(tutorJson);
                    if (doc.RootElement.TryGetProperty("fullName", out var nameProp))
                        tutorName = nameProp.GetString() ?? "Unknown";
                    if (doc.RootElement.TryGetProperty("specialization", out var specProp))
                        tutorSpecialization = specProp.GetString() ?? "";
                }

                // Get review if exists
                var review = await _context.Reviews.FirstOrDefaultAsync(r => r.BookingID == booking.BookingID);

                result.Add(new BookingWithDetails
                {
                    BookingID = booking.BookingID,
                    StudentID = booking.StudentID,
                    StudentName = studentName,
                    TutorID = booking.TutorID,
                    TutorName = tutorName,
                    TutorSpecialization = tutorSpecialization,
                    BookingDate = booking.BookingDate,
                    DurationHours = booking.DurationHours,
                    TotalAmount = booking.TotalAmount,
                    Status = booking.Status,
                    Topic = booking.Topic,
                    MeetingLink = booking.MeetingLink,
                    ReviewComment = review?.Comment,
                    Rating = review?.Rating
                });
            }

            return Ok(result);
        }

        // PUT: api/Booking/Confirm/{id}
        [HttpPut("Confirm/{id}")]
        public async Task<ActionResult> Confirm(string id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();
            return Ok(booking);
        }

        // PUT: api/Booking/Complete/{id}
        [HttpPut("Complete/{id}")]
        public async Task<ActionResult> Complete(string id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            booking.Status = "Completed";
            await _context.SaveChangesAsync();
            return Ok(booking);
        }

        // PUT: api/Booking/Cancel/{id}
        [HttpPut("Cancel/{id}")]
        public async Task<ActionResult> Cancel(string id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            // Free up the time slot
            if (!string.IsNullOrEmpty(booking.SlotID))
            {
                var tutorServiceUrl = _configuration["ServiceUrls:TutorService"] ?? "https://localhost:7228";
                var slotClient = _httpClientFactory.CreateClient();
                await slotClient.PutAsync($"{tutorServiceUrl}/api/Tutor/FreeSlot/{booking.SlotID}", null);
            }

            return Ok(booking);
        }

        // POST: api/Booking/AddReview
        [HttpPost("AddReview")]
        public async Task<ActionResult<Review>> AddReview([FromBody] ReviewRequest request)
        {
            try
            {
                Console.WriteLine($"=== ADD REVIEW ===");
                Console.WriteLine($"BookingID: {request.BookingID}");
                Console.WriteLine($"StudentID: {request.StudentID}");
                Console.WriteLine($"TutorID: {request.TutorID}");
                Console.WriteLine($"Rating: {request.Rating}");

                var reviewErrors = ReviewValidator.Validate(request);
                if (reviewErrors.Any())
                    return BadRequest(new { success = false, errors = reviewErrors });

                var newReview = new Review
                {
                    ReviewID = Guid.NewGuid().ToString(),
                    BookingID = request.BookingID,
                    StudentID = request.StudentID,
                    TutorID = request.TutorID,
                    Rating = request.Rating,
                    Comment = request.Comment ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(newReview);
                await _context.SaveChangesAsync();

                return Ok(newReview);
            }
            catch (DbUpdateException dbEx)
            {
                // This will show the actual database error
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                Console.WriteLine($"DB ERROR: {innerMessage}");
                return StatusCode(500, new { message = innerMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/Booking/GetTutorReviews/{tutorId}
        [HttpGet("GetTutorReviews/{tutorId}")]
        public async Task<ActionResult<IEnumerable<Review>>> GetTutorReviews(string tutorId)
        {
            var reviews = await _context.Reviews.Where(r => r.TutorID == tutorId).ToListAsync();
            return Ok(reviews);
        }

        // GET: api/Booking/GetAverageRating/{tutorId}
        [HttpGet("GetAverageRating/{tutorId}")]
        public async Task<ActionResult<object>> GetAverageRating(string tutorId)
        {
            var reviews = await _context.Reviews.Where(r => r.TutorID == tutorId).ToListAsync();
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            return Ok(new { averageRating, totalReviews = reviews.Count });
        }
    }
}