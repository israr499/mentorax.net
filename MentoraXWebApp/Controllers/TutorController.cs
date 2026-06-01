using Microsoft.AspNetCore.Mvc;
using MentoraXWebApp.Models;
using System.Text.Json;

namespace MentoraXWebApp.Controllers
{
    public class TutorController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TutorController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private bool IsTutorLoggedIn()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Tutor";
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsTutorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetString("UserId");
            var client = _httpClientFactory.CreateClient("Gateway");

            // Get tutor profile - CHANGED: api/tutors/GetByUser → api/Tutor/GetByUser
            var tutorResponse = await client.GetAsync($"api/Tutor/GetByUser/{userId}");

            if (!tutorResponse.IsSuccessStatusCode)
                return RedirectToAction("CompleteProfile");

            var tutorJson = await tutorResponse.Content.ReadAsStringAsync();
            var tutor = JsonSerializer.Deserialize<TutorProfile>(tutorJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Get bookings - CHANGED: api/bookings/GetBookingsWithDetails → api/Booking/GetBookingsWithDetails
            var bookingsResponse = await client.GetAsync($"api/Booking/GetBookingsWithDetails?tutorId={tutor.TutorID}");
            List<Booking> bookings = new List<Booking>();

            if (bookingsResponse.IsSuccessStatusCode)
            {
                var bookingsJson = await bookingsResponse.Content.ReadAsStringAsync();
                bookings = JsonSerializer.Deserialize<List<Booking>>(bookingsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            ViewBag.Tutor = tutor;
            return View(bookings);
        }

        [HttpGet]
        public IActionResult CompleteProfile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CompleteProfile(TutorRegisterViewModel model)
        {
            var userId = HttpContext.Session.GetString("UserId");

            var tutorProfile = new
            {
                UserID = userId,
                model.Specialization,
                model.Qualification,
                model.ExperienceYears,
                model.HourlyRate,
                model.Bio,
                model.TeachingStyle,
                model.EducationDetails,
                Subjects = model.Subjects ?? new List<TutorSubjectInput>(),
                Availability = model.Availability ?? new List<TutorAvailabilityInput>()
            };

            var client = _httpClientFactory.CreateClient("Gateway");
            // CHANGED: api/tutors/CreateProfile → api/Tutor/CreateProfile
            var response = await client.PostAsJsonAsync("api/Tutor/CreateProfile", tutorProfile);

            if (response.IsSuccessStatusCode)
            {
                ViewBag.Success = "Profile submitted! Waiting for admin approval.";
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Failed to create profile";
            return View(model);
        }

        public async Task<IActionResult> MyBookings()
        {
            if (!IsTutorLoggedIn())
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetString("UserId");
            var client = _httpClientFactory.CreateClient("Gateway");

            // CHANGED: api/tutors/GetByUser → api/Tutor/GetByUser
            var tutorResponse = await client.GetAsync($"api/Tutor/GetByUser/{userId}");
            if (!tutorResponse.IsSuccessStatusCode)
                return View(new List<Booking>());

            var tutorJson = await tutorResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(tutorJson);
            var tutorId = doc.RootElement.GetProperty("tutorID").GetString();

            // CHANGED: api/bookings/GetBookingsWithDetails → api/Booking/GetBookingsWithDetails
            var response = await client.GetAsync($"api/Booking/GetBookingsWithDetails?tutorId={tutorId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var bookings = JsonSerializer.Deserialize<List<Booking>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return View(bookings);
            }

            return View(new List<Booking>());
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(string bookingId)
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            // CHANGED: api/bookings/Confirm → api/Booking/Confirm
            await client.PutAsync($"api/Booking/Confirm/{bookingId}", null);
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteBooking(string bookingId)
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            // CHANGED: api/bookings/Complete → api/Booking/Complete
            await client.PutAsync($"api/Booking/Complete/{bookingId}", null);
            return RedirectToAction("MyBookings");
        }

        // ── Revenue data for the tutor dashboard (called via AJAX) ──────
        [HttpGet]
        public async Task<IActionResult> GetRevenue()
        {
            if (!IsTutorLoggedIn())
                return Unauthorized();

            var userId = HttpContext.Session.GetString("UserId");
            var client = _httpClientFactory.CreateClient("Gateway");

            // Get TutorID first
            var tutorResponse = await client.GetAsync($"api/Tutor/GetByUser/{userId}");
            if (!tutorResponse.IsSuccessStatusCode)
                return Json(new { success = false, message = "Tutor profile not found." });

            var tutorJson = await tutorResponse.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(tutorJson);
            var tutorId = doc.RootElement.GetProperty("tutorID").GetString();

            var revenueResponse = await client.GetAsync($"api/Revenue/tutor/{tutorId}");
            if (revenueResponse.IsSuccessStatusCode)
            {
                var json = await revenueResponse.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }

            return Json(new { success = false, message = "Could not load revenue data." });
        }
    }
}