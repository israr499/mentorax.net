using Microsoft.AspNetCore.Mvc;
using MentoraXWebApp.Models;
using MentoraXWebApp.Services;
using System.Text.Json;

namespace MentoraXWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailService _emailService;

        private const string PendingUserIdKey = "OTP_PendingUserId";
        private const string PendingEmailKey = "OTP_PendingEmail";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AccountController(IHttpClientFactory httpClientFactory,
                                 ILogger<AccountController> logger,
                                 IEmailService emailService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _emailService = emailService;
        }

        // ── LOGOUT ──────────────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ── LOGIN (GET) ──────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── LOGIN (POST) ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.PostAsJsonAsync("api/User/Login", model);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View(model);
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            LoginApiResponse? loginResult;

            try
            {
                loginResult = JsonSerializer.Deserialize<LoginApiResponse>(jsonString, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize login response");
                ViewBag.Error = "An unexpected error occurred. Please try again.";
                return View(model);
            }

            if (loginResult == null)
            {
                ViewBag.Error = "An unexpected error occurred. Please try again.";
                return View(model);
            }

            // Admin (RequiresOtp=false) => direct login, no OTP
            if (!loginResult.RequiresOtp)
            {
                SetUserSession(loginResult.Token!, loginResult.UserID!, loginResult.Email!,
                               loginResult.FullName!, loginResult.Role!, loginResult.ProfileImageUrl);
                _logger.LogInformation("Direct login: {Email} Role={Role}", loginResult.Email, loginResult.Role);
                return RedirectToDashboard(loginResult.Role!);
            }

            // All other users => send OTP email then go to verify page
            try
            {
                await _emailService.SendOtpEmailAsync(loginResult.Email!, loginResult.OtpCode!);
                _logger.LogInformation("OTP email sent to {Email}", loginResult.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", loginResult.Email);
                ViewBag.Error = "Could not send OTP email. Please check your email address or try again.";
                return View(model);
            }

            HttpContext.Session.SetString(PendingUserIdKey, loginResult.PendingUserId!);
            HttpContext.Session.SetString(PendingEmailKey, loginResult.Email!);

            return RedirectToAction("VerifyOtp");
        }

        // ── VERIFY OTP (GET) ─────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var pendingUserId = HttpContext.Session.GetString(PendingUserIdKey);
            if (string.IsNullOrEmpty(pendingUserId))
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            var email = HttpContext.Session.GetString(PendingEmailKey) ?? "";
            var vm = new VerifyOtpViewModel
            {
                PendingUserId = pendingUserId,
                MaskedEmail = MaskEmail(email)
            };
            return View(vm);
        }

        // ── VERIFY OTP (POST) ────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            var pendingUserId = HttpContext.Session.GetString(PendingUserIdKey);
            if (string.IsNullOrEmpty(pendingUserId))
            {
                TempData["Error"] = "Session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(model.OtpCode))
            {
                model.PendingUserId = pendingUserId;
                model.MaskedEmail = MaskEmail(HttpContext.Session.GetString(PendingEmailKey) ?? "");
                ModelState.AddModelError("OtpCode", "Please enter the OTP code.");
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("Gateway");
            var payload = new { PendingUserId = pendingUserId, OtpCode = model.OtpCode.Trim() };

            var response = await client.PostAsJsonAsync("api/User/VerifyOtp", payload);
            var jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string apiMessage = "Invalid or expired OTP. Please try again.";
                try
                {
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        apiMessage = msgProp.GetString() ?? apiMessage;
                }
                catch { /* ignore */ }

                model.PendingUserId = pendingUserId;
                model.MaskedEmail = MaskEmail(HttpContext.Session.GetString(PendingEmailKey) ?? "");
                ViewBag.Error = apiMessage;
                return View(model);
            }

            AuthApiResponse? authResult;
            try
            {
                authResult = JsonSerializer.Deserialize<AuthApiResponse>(jsonString, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize VerifyOtp response");
                ViewBag.Error = "An unexpected error occurred after OTP verification.";
                return View(model);
            }

            if (authResult == null)
            {
                ViewBag.Error = "Unexpected server response. Please try again.";
                return View(model);
            }

            HttpContext.Session.Remove(PendingUserIdKey);
            HttpContext.Session.Remove(PendingEmailKey);

            SetUserSession(authResult.Token, authResult.UserID, authResult.Email,
                           authResult.FullName, authResult.Role, authResult.ProfileImageUrl);

            _logger.LogInformation("OTP verified: {Email} Role={Role}", authResult.Email, authResult.Role);
            return RedirectToDashboard(authResult.Role);
        }

        // ── RESEND OTP ───────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ResendOtp()
        {
            var pendingUserId = HttpContext.Session.GetString(PendingUserIdKey);
            if (string.IsNullOrEmpty(pendingUserId))
                return Json(new { success = false, message = "Session expired. Please log in again." });

            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.PostAsync($"api/User/ResendOtp/{pendingUserId}", null);

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = "Failed to resend OTP. Please try again." });

            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var otpCode = doc.RootElement.GetProperty("otpCode").GetString()!;
                var email = doc.RootElement.GetProperty("email").GetString()!;

                await _emailService.SendOtpEmailAsync(email, otpCode);
                return Json(new { success = true, message = "A new OTP has been sent to your email." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResendOtp email failed");
                return Json(new { success = false, message = "OTP regenerated but email could not be sent." });
            }
        }

        // ── REGISTER (landing page) ──────────────────────────────────────────────
        [HttpGet]
        public IActionResult Register() => View();

        // ── REGISTER STUDENT (GET) ───────────────────────────────────────────────
        [HttpGet]
        public IActionResult RegisterStudent() => View();

        // ── REGISTER STUDENT (POST) ──────────────────────────────────────────────
        // FIX: Was redirecting to Login on success without setting session.
        //      Also was dumping raw JSON into ViewBag.Error on failure.
        [HttpPost]
        public async Task<IActionResult> RegisterStudent(StudentRegisterViewModel model)
        {
            var client = _httpClientFactory.CreateClient("Gateway");
            var response = await client.PostAsJsonAsync("api/User/RegisterStudent", model);
            var jsonString = await response.Content.ReadAsStringAsync();

            // ── Failure path ─────────────────────────────────────────────────────
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = ExtractApiErrorMessage(jsonString, "Registration failed. Please check your details and try again.");
                _logger.LogWarning("RegisterStudent failed ({Status}): {Body}", (int)response.StatusCode, jsonString);
                return View(model);
            }

            // ── Success path ─────────────────────────────────────────────────────
            AuthApiResponse? result;
            try
            {
                result = JsonSerializer.Deserialize<AuthApiResponse>(jsonString, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize RegisterStudent response");
                ViewBag.Error = "Registration succeeded but we could not log you in automatically. Please log in.";
                return RedirectToAction("Login");
            }

            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                _logger.LogWarning("RegisterStudent returned 200 but token was missing. Body: {Body}", jsonString);
                ViewBag.Error = "Registration succeeded but no token was returned. Please log in.";
                return RedirectToAction("Login");
            }

            // Set session so the user is immediately logged in
            SetUserSession(result.Token, result.UserID, result.Email,
                           result.FullName, result.Role, result.ProfileImageUrl);

            _logger.LogInformation("Student registered and logged in: {Email}", result.Email);

            // Redirect straight to the student dashboard
            return RedirectToDashboard(result.Role);
        }

        // ── REGISTER TUTOR (GET) ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult RegisterTutor() => View();

        // ── REGISTER TUTOR (POST) ────────────────────────────────────────────────
        // FIX 1: Was showing hardcoded "User registration failed." instead of the
        //         real API error (e.g. duplicate email).
        // FIX 2: Was redirecting to Login on success without setting the session,
        //         so the user was never actually logged in.
        // FIX 3: If tutor-profile creation fails, user registration already
        //         succeeded — we now log the user in and surface a soft warning
        //         instead of leaving them stuck on the registration page.
        [HttpPost]
        public async Task<IActionResult> RegisterTutor(TutorRegisterViewModel model)
        {
            var client = _httpClientFactory.CreateClient("Gateway");

            // ── Step 1: Create the user account ──────────────────────────────────
            var registerData = new
            {
                model.Email,
                model.Password,
                model.FullName,
                Role = "Tutor",
                model.PhoneNumber
            };

            var registerResponse = await client.PostAsJsonAsync("api/User/Register", registerData);
            var registerJson = await registerResponse.Content.ReadAsStringAsync();

            if (!registerResponse.IsSuccessStatusCode)
            {
                // Show the actual API error (e.g. "Email already exists") instead of
                // a hardcoded generic string.
                ViewBag.Error = ExtractApiErrorMessage(registerJson, "User registration failed. Please check your details and try again.");
                _logger.LogWarning("RegisterTutor user-creation failed ({Status}): {Body}",
                                   (int)registerResponse.StatusCode, registerJson);
                return View(model);
            }

            AuthApiResponse? user;
            try
            {
                user = JsonSerializer.Deserialize<AuthApiResponse>(registerJson, JsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Register response during tutor registration");
                ViewBag.Error = "Registration succeeded but an unexpected error occurred. Please log in.";
                return RedirectToAction("Login");
            }

            if (user == null || string.IsNullOrEmpty(user.Token))
            {
                _logger.LogWarning("RegisterTutor: user API returned 200 but token was missing. Body: {Body}", registerJson);
                ViewBag.Error = "Account created but no token was returned. Please log in.";
                return RedirectToAction("Login");
            }

            // ── Step 2: Create the tutor profile ─────────────────────────────────
            var tutorProfile = new
            {
                UserID = user.UserID,
                model.Specialization,
                model.Qualification,
                model.ExperienceYears,
                model.HourlyRate,
                model.Bio,
                model.TeachingStyle,
                model.EducationDetails,
                model.IntroVideoUrl,
                Subjects = model.Subjects,
                Availability = model.Availability
            };

            var tutorResponse = await client.PostAsJsonAsync("api/Tutor/CreateProfile", tutorProfile);

            // Set the user session regardless — the account exists at this point.
            SetUserSession(user.Token, user.UserID, user.Email,
                           user.FullName, user.Role, user.ProfileImageUrl);

            if (!tutorResponse.IsSuccessStatusCode)
            {
                var tutorError = await tutorResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("RegisterTutor: CreateProfile failed ({Status}): {Body}",
                                   (int)tutorResponse.StatusCode, tutorError);

                // Account is created and session is set. Redirect to dashboard with
                // a soft warning — the tutor can complete their profile later.
                TempData["Warning"] = "Your account was created but your tutor profile could not be saved. " +
                                      "Please complete your profile from the dashboard.";
                return RedirectToDashboard(user.Role);
            }

            _logger.LogInformation("Tutor registered and profile created: {Email}", user.Email);

            // Full success — go straight to dashboard (not Login)
            TempData["Success"] = "Welcome to MentoraX! Your profile is under review by our admins.";
            return RedirectToDashboard(user.Role);
        }

        // ── HELPERS ──────────────────────────────────────────────────────────────

        private void SetUserSession(string token, string userId, string email,
                                    string fullName, string role, string? profileImageUrl)
        {
            HttpContext.Session.SetString("UserId", userId);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserFullName", fullName);
            HttpContext.Session.SetString("Token", token);

            if (!string.IsNullOrEmpty(profileImageUrl))
                HttpContext.Session.SetString("ProfileImageUrl", profileImageUrl);
        }

        private IActionResult RedirectToDashboard(string role) => role switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "Tutor" => RedirectToAction("Dashboard", "Tutor"),
            _ => RedirectToAction("Index", "Student")
        };

        /// <summary>
        /// Tries to extract a human-readable error message from an API JSON error body.
        /// Falls back to <paramref name="fallback"/> if parsing fails.
        /// </summary>
        private static string ExtractApiErrorMessage(string jsonBody, string fallback)
        {
            if (string.IsNullOrWhiteSpace(jsonBody)) return fallback;
            try
            {
                using var doc = JsonDocument.Parse(jsonBody);
                var root = doc.RootElement;

                // Try "message" property first (our standard error shape)
                if (root.TryGetProperty("message", out var msgProp))
                {
                    var msg = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(msg)) return msg;
                }

                // Try "title" (ASP.NET validation problem details)
                if (root.TryGetProperty("title", out var titleProp))
                {
                    var title = titleProp.GetString();
                    if (!string.IsNullOrWhiteSpace(title)) return title;
                }
            }
            catch { /* ignore parse errors */ }

            return fallback;
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "your email";
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return email;
            return email[0] + new string('*', Math.Max(atIndex - 2, 1)) + email[(atIndex - 1)..];
        }
    }
}