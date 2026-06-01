using Microsoft.AspNetCore.Mvc;
using MentoraXWebApp.Models;
using System.Text.Json;

namespace MentoraXWebApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StudentController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private bool IsStudentLoggedIn()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Student";
        }

        // ── HELPER ───────────────────────────────────────────────────────────────
        // Fetches FullName directly from UserService via the Gateway.
        // Needed because TutorService.GetFullProfile does a service-to-service HTTPS
        // call that silently fails in local dev (self-signed cert rejection), causing
        // its fallback  string fullName = tutor.UserID  to return the UUID as the name.
        private async Task<string> ResolveFullNameAsync(HttpClient gatewayClient, string userId)
        {
            try
            {
                var resp = await gatewayClient.GetAsync($"api/User/GetById/{userId}");
                if (!resp.IsSuccessStatusCode) return "";

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                // ASP.NET default serializer → camelCase "fullName"
                if (doc.RootElement.TryGetProperty("fullName", out var p1)) return p1.GetString() ?? "";
                // fallback for PascalCase
                if (doc.RootElement.TryGetProperty("FullName", out var p2)) return p2.GetString() ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResolveFullName] userId={userId}: {ex.Message}");
            }
            return "";
        }

        // ── INDEX (Browse Tutors) ─────────────────────────────────────────────────
        public async Task<IActionResult> Index(string searchSubject = "")
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            string url = string.IsNullOrEmpty(searchSubject)
                ? "api/Tutor/GetApproved"
                : $"api/Tutor/SearchBySubject?subject={Uri.EscapeDataString(searchSubject)}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.SearchSubject = searchSubject;
                return View(new List<TutorProfile>());
            }

            var tutors = JsonSerializer.Deserialize<List<TutorProfile>>(
                             await response.Content.ReadAsStringAsync(), opts)
                         ?? new List<TutorProfile>();

            foreach (var tutor in tutors)
            {
                // Fix UUID name
                if (string.IsNullOrWhiteSpace(tutor.FullName) || tutor.FullName == tutor.UserID)
                    tutor.FullName = await ResolveFullNameAsync(client, tutor.UserID);

                // Ratings
                try
                {
                    var ratingResp = await client.GetAsync($"api/Booking/GetAverageRating/{tutor.TutorID}");
                    if (ratingResp.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await ratingResp.Content.ReadAsStringAsync());
                        tutor.AverageRating = doc.RootElement.GetProperty("averageRating").GetDouble();
                        tutor.TotalReviews = doc.RootElement.GetProperty("totalReviews").GetInt32();
                    }
                }
                catch { /* ratings non-critical */ }
            }

            ViewBag.SearchSubject = searchSubject;
            return View(tutors);
        }

        // ── TUTOR DETAILS ─────────────────────────────────────────────────────────
        public async Task<IActionResult> TutorDetails(string id)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var response = await client.GetAsync($"api/Tutor/GetFullProfile/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Tutor not found.";
                return View(new TutorProfile());
            }

            var tutor = JsonSerializer.Deserialize<TutorProfile>(
                            await response.Content.ReadAsStringAsync(), opts)
                        ?? new TutorProfile();

            // ── NAME FIX ──────────────────────────────────────────────────────────
            // TutorService.GetFullProfile tries to call UserService via direct HTTPS.
            // That call silently fails in local dev (self-signed cert rejection) and
            // falls back to:  string fullName = tutor.UserID;
            // So FullName arrives here as the UUID.  We re-fetch it through the Gateway
            // which already works for every other call in this app.
            if (string.IsNullOrWhiteSpace(tutor.FullName) || tutor.FullName == tutor.UserID)
            {
                var name = await ResolveFullNameAsync(client, tutor.UserID);
                if (!string.IsNullOrWhiteSpace(name))
                    tutor.FullName = name;
            }
            // ─────────────────────────────────────────────────────────────────────

            // Available time slots
            var slotsResp = await client.GetAsync($"api/Tutor/GetAvailableSlots/{id}");
            ViewBag.AvailableSlots = slotsResp.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<TimeSlot>>(
                      await slotsResp.Content.ReadAsStringAsync(), opts)
                  ?? new List<TimeSlot>()
                : new List<TimeSlot>();

            // Reviews
            var reviewsResp = await client.GetAsync($"api/Booking/GetTutorReviews/{id}");
            ViewBag.Reviews = reviewsResp.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<ReviewRequest>>(
                      await reviewsResp.Content.ReadAsStringAsync(), opts)
                  ?? new List<ReviewRequest>()
                : new List<ReviewRequest>();

            return View(tutor);
        }

        // ── BOOK SESSION GET ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> BookSession(string tutorId, string slotId, DateTime? slotDate)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var tutorResponse = await client.GetAsync($"api/Tutor/GetById/{tutorId}");
            if (!tutorResponse.IsSuccessStatusCode) return NotFound();

            var tutor = JsonSerializer.Deserialize<TutorProfile>(
                            await tutorResponse.Content.ReadAsStringAsync(), opts);

            if (tutor != null && (string.IsNullOrWhiteSpace(tutor.FullName) || tutor.FullName == tutor.UserID))
                tutor.FullName = await ResolveFullNameAsync(client, tutor.UserID);

            ViewBag.Tutor = tutor;
            ViewBag.TutorId = tutorId;
            ViewBag.TutorName = tutor?.FullName;
            ViewBag.HourlyRate = tutor?.HourlyRate;
            ViewBag.SlotId = slotId;
            ViewBag.SlotDate = slotDate?.ToString("yyyy-MM-dd HH:mm");
            return View();
        }

        // ── BOOK SESSION POST ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> BookSession(BookingRequest request)
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var client = _httpClientFactory.CreateClient("Gateway");

                Console.WriteLine($"=== BOOKING DEBUG ===");
                Console.WriteLine($"UserId from session: {userId}");

                var studentResponse = await client.GetAsync($"api/User/GetStudent/{userId}");
                var studentJson = await studentResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Student API Response: {studentJson}");

                if (studentResponse.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(studentJson);
                    request.StudentID = doc.RootElement.GetProperty("studentID").GetString();
                    Console.WriteLine($"StudentID set to: {request.StudentID}");
                }
                else
                {
                    ViewBag.Error = "Could not find student profile. Please contact support.";
                    return View(request);
                }

                var response = await client.PostAsJsonAsync("api/Booking/Create", request);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Booking Response: {response.StatusCode} — {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Booking created successfully!";
                    return RedirectToAction("MyBookings");
                }

                ViewBag.Error = $"Booking failed: {responseContent}";
                return View(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                ViewBag.Error = $"Booking failed: {ex.Message}";
                return View(request);
            }
        }

        // ── MY BOOKINGS ───────────────────────────────────────────────────────────
        public async Task<IActionResult> MyBookings()
        {
            if (!IsStudentLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetString("UserId");
            var client = _httpClientFactory.CreateClient("Gateway");
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var studentResponse = await client.GetAsync($"api/User/GetStudent/{userId}");
            if (!studentResponse.IsSuccessStatusCode)
                return View(new List<Booking>());

            using var sDoc = JsonDocument.Parse(await studentResponse.Content.ReadAsStringAsync());
            var studentId = sDoc.RootElement.GetProperty("studentID").GetString();

            var response = await client.GetAsync($"api/Booking/GetBookingsWithDetails?studentId={studentId}");
            if (!response.IsSuccessStatusCode)
                return View(new List<Booking>());

            var bookings = JsonSerializer.Deserialize<List<Booking>>(
                               await response.Content.ReadAsStringAsync(), opts)
                           ?? new List<Booking>();

            return View(bookings);
        }

        // ── CANCEL BOOKING ────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CancelBooking(string bookingId)
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            await client.PutAsync($"api/Booking/Cancel/{bookingId}", null);
            return RedirectToAction("MyBookings");
        }

        // ── ADD REVIEW ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> AddReview(ReviewRequest review)
        {
            try
            {
                review.StudentID = HttpContext.Session.GetString("UserId");
                Console.WriteLine($"=== ADD REVIEW DEBUG ===  BookingID={review.BookingID}  TutorID={review.TutorID}  Rating={review.Rating}");

                var client = _httpClientFactory.CreateClient("Gateway");
                var response = await client.PostAsJsonAsync("api/Booking/AddReview", review);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Review Response: {response.StatusCode} — {content}");

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode ? "Review submitted successfully!" : $"Failed: {content}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("MyBookings");
        }
    }
}