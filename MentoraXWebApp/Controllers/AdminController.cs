using Microsoft.AspNetCore.Mvc;
using MentoraXWebApp.Models;
using System.Text.Json;

namespace MentoraXWebApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var model = new AdminDashboardViewModel();

            var pendingResponse = await client.GetAsync("api/Tutor/GetPending");
            if (pendingResponse.IsSuccessStatusCode)
            {
                var json = await pendingResponse.Content.ReadAsStringAsync();
                model.PendingTutors = JsonSerializer.Deserialize<List<TutorProfile>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var approvedResponse = await client.GetAsync("api/Tutor/GetApproved");
            if (approvedResponse.IsSuccessStatusCode)
            {
                var json = await approvedResponse.Content.ReadAsStringAsync();
                model.ApprovedTutors = JsonSerializer.Deserialize<List<TutorProfile>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var bookingsResponse = await client.GetAsync("api/Booking/GetBookingsWithDetails");
            if (bookingsResponse.IsSuccessStatusCode)
            {
                var json = await bookingsResponse.Content.ReadAsStringAsync();
                model.RecentBookings = JsonSerializer.Deserialize<List<Booking>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                model.TotalBookings = model.RecentBookings?.Count ?? 0;
                model.TotalRevenue = model.RecentBookings?.Sum(b => b.TotalAmount) ?? 0;
            }

            var usersResponse = await client.GetAsync("api/User/GetAll");
            if (usersResponse.IsSuccessStatusCode)
            {
                var json = await usersResponse.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                model.TotalStudents = users?.Count(u => u.Role == "Student") ?? 0;
                model.TotalTutors = users?.Count(u => u.Role == "Tutor") ?? 0;
                model.RecentUsers = users?.Take(10).ToList();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveTutor(string tutorId, bool isApproved, string rejectionReason = "")
        {
            var adminId = HttpContext.Session.GetString("UserId");
            var approvalRequest = new
            {
                TutorID = tutorId,
                AdminID = adminId,
                IsApproved = isApproved,
                RejectionReason = rejectionReason
            };

            var client = _httpClientFactory.CreateClient("Gateway");
            await client.PutAsJsonAsync("api/Tutor/Approve", approvalRequest);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> TutorDetails(string id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var client = _httpClientFactory.CreateClient("Gateway");
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Step 1: try treating id as a TutorID directly
            var profileResponse = await client.GetAsync($"api/Tutor/GetFullProfile/{id}");

            if (!profileResponse.IsSuccessStatusCode)
            {
                // Step 2: id might be a UserID — resolve to TutorID
                var byUserResponse = await client.GetAsync($"api/Tutor/GetByUser/{id}");

                if (!byUserResponse.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "No tutor profile found for this user.";
                    return View("TutorNotFound");
                }

                var byUserJson = await byUserResponse.Content.ReadAsStringAsync();
                var tutorRecord = JsonSerializer.Deserialize<TutorProfile>(byUserJson, opts);

                if (tutorRecord?.TutorID == null)
                {
                    ViewBag.ErrorMessage = "Tutor record found but TutorID is missing.";
                    return View("TutorNotFound");
                }

                // Step 3: fetch full profile with the real TutorID
                profileResponse = await client.GetAsync($"api/Tutor/GetFullProfile/{tutorRecord.TutorID}");

                if (!profileResponse.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = $"Tutor profile incomplete. TutorID: {tutorRecord.TutorID}";
                    return View("TutorNotFound");
                }
            }

            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            var tutor = JsonSerializer.Deserialize<TutorProfile>(profileJson, opts);
            return View(tutor);
        }

        // FIX: Added [IgnoreAntiforgeryToken] because the fetch() calls from AllUsers.cshtml
        // do not include the antiforgery request header. Without this attribute, ASP.NET Core
        // returns 400 Bad Request which the view may misinterpret. Alternatively, you can add
        // the X-XSRF-TOKEN header in the JS fetch calls.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] JsonElement data)
        {
            if (!IsAdminLoggedIn())
                return Unauthorized(new { success = false, message = "Not authorized" });

            try
            {
                var newPassword = data.GetProperty("password").GetString();
                var client = _httpClientFactory.CreateClient("Gateway");
                var response = await client.PutAsJsonAsync($"api/User/ResetPassword/{id}", new { password = newPassword });

                if (response.IsSuccessStatusCode)
                    return Ok(new { success = true });

                return BadRequest(new { success = false, message = "Failed to reset password" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // FIX BUG 5: Deactivate was returning 404.
        // Root causes:
        //   1. Missing [IgnoreAntiforgeryToken] — fetch() doesn't send antiforgery header by default,
        //      causing ASP.NET to reject with 400 which looked like 404 in the browser.
        //   2. The route must be POST /Admin/Deactivate/{id} which this already matches.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            if (!IsAdminLoggedIn())
                return Unauthorized(new { success = false, message = "Not authorized" });

            if (string.IsNullOrEmpty(id))
                return BadRequest(new { success = false, message = "User ID is required" });

            try
            {
                var client = _httpClientFactory.CreateClient("Gateway");
                var response = await client.PutAsync($"api/User/Deactivate/{id}", null);

                if (response.IsSuccessStatusCode)
                    return Ok(new { success = true, message = "User deactivated successfully" });

                var error = await response.Content.ReadAsStringAsync();
                return BadRequest(new { success = false, message = $"Failed to deactivate user: {error}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin.Deactivate] Error: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // FIX BUG 5: Same fix applied to Activate.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            if (!IsAdminLoggedIn())
                return Unauthorized(new { success = false, message = "Not authorized" });

            if (string.IsNullOrEmpty(id))
                return BadRequest(new { success = false, message = "User ID is required" });

            try
            {
                var client = _httpClientFactory.CreateClient("Gateway");
                var response = await client.PutAsync($"api/User/Activate/{id}", null);

                if (response.IsSuccessStatusCode)
                    return Ok(new { success = true, message = "User activated successfully" });

                var error = await response.Content.ReadAsStringAsync();
                return BadRequest(new { success = false, message = $"Failed to activate user: {error}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin.Activate] Error: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> AllBookings()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.GetAsync("api/Booking/GetBookingsWithDetails");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var bookings = JsonSerializer.Deserialize<List<Booking>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(bookings);
            }

            return View(new List<Booking>());
        }

        public async Task<IActionResult> AllUsers()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.GetAsync("api/User/GetAll");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(users);
            }

            return View(new List<User>());
        }

        // ── Revenue Report ──────────────────────────────────────────────
        public async Task<IActionResult> Revenue(string fromDate = "", string toDate = "")
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.FromDate = string.IsNullOrEmpty(fromDate)
                ? DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd")
                : fromDate;
            ViewBag.ToDate = string.IsNullOrEmpty(toDate)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                : toDate;

            return View();
        }

        // ── Revenue CSV download (proxied from BookingService) ──────────
        public async Task<IActionResult> DownloadRevenueCsv(string fromDate, string toDate)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "Account");

            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.GetAsync($"api/Revenue/admin/csv-report?fromDate={fromDate}&toDate={toDate}");

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                return File(bytes, "text/csv", $"MentoraX_Revenue_{fromDate}_{toDate}.csv");
            }

            TempData["Error"] = "Failed to generate CSV report.";
            return RedirectToAction("Revenue");
        }
    }
}
