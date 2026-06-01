using System.ComponentModel.DataAnnotations;

namespace UserService.Models
{
    // ====================================================================
    // DOMAIN ENTITY — stored in Users table
    // ====================================================================
    public class User
    {
        [Key]
        public string UserID { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Role { get; set; }  // Student | Tutor | Admin

        public string? ProfileImageUrl { get; set; }

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Legacy flag ───────────────────────────────────────────────
        // TRUE  = registered before OTP was introduced → skip OTP on login.
        // FALSE = new user → OTP required.
        public bool IsLegacyUser { get; set; } = false;

        // ── Two-Factor / OTP ──────────────────────────────────────────
        public string? TwoFactorSecret { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? LastUsedOTP { get; set; }

        // Active OTP sent to email (6-digit code + expiry)
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }

        // FIX 1: was declared as  public string IsOtpVerified  in the new document
        // but UserController writes  user.IsOtpVerified = false/true  (bool assignment).
        // Declaring it as string causes CS0029 compile error. Keep it as bool.
        public bool IsOtpVerified { get; set; } = false;

        // ── Remember Device ───────────────────────────────────────────
        // FIX 2: new document renamed these to RememberDeviceToken /
        // RememberDeviceExpiry, but UserController still reads and writes
        // the OLD names  RememberToken  and  RememberTokenExpiry.
        // We keep the original names so the controller compiles without changes,
        // and add the new-name properties as aliases pointing at the same data.
        public string? RememberToken { get; set; }
        public DateTime? RememberTokenExpiry { get; set; }

        // Aliases — these are what the new document calls them.
        // They delegate to the real backing properties so both names work.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? RememberDeviceToken
        {
            get => RememberToken;
            set => RememberToken = value;
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime? RememberDeviceExpiry
        {
            get => RememberTokenExpiry;
            set => RememberTokenExpiry = value;
        }
    }

    // ====================================================================
    // STUDENT PROFILE — stored in Students table
    // ====================================================================
    public class Student
    {
        [Key]
        public string StudentID { get; set; }
        public string UserID { get; set; }
        public string? EducationLevel { get; set; }
        public string? SubjectsOfInterest { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? GradeLevel { get; set; }
    }

    // ====================================================================
    // REQUEST / RESPONSE DTOs
    // ====================================================================
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class StudentRegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string EducationLevel { get; set; }
        public string SubjectsOfInterest { get; set; }
        public string GradeLevel { get; set; }
    }

    // Original AuthResponse — used by RegisterStudent, Register, Login endpoints
    public class AuthResponse
    {
        public string Token { get; set; }
        public string UserID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string? ProfileImageUrl { get; set; }
        // Present only when client requested RememberDevice during OTP verify
        public string? RememberDeviceToken { get; set; }
    }

    // ── NEW DTOs (from updated document) ─────────────────────────────────

    /// <summary>
    /// Returned by POST api/User/Login.
    /// RequiresOtp=true  → frontend redirects to OTP page.
    /// RequiresOtp=false → legacy/remember-device, full token returned directly.
    /// </summary>
    public class LoginResponse
    {
        public bool RequiresOtp { get; set; }

        // Present when RequiresOtp = true (OTP flow)
        public string? PendingUserId { get; set; }
        public string? OtpCode { get; set; }   // returned so WebApp can email it

        // Present for both cases
        public string? Email { get; set; }

        // Present when RequiresOtp = false (admin direct login)
        public string? Token { get; set; }
        public string? UserID { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>Sent by POST api/User/VerifyOtp.</summary>
    public class VerifyOtpRequest
    {
        public string PendingUserId { get; set; }
        public string OtpCode { get; set; }
        public bool RememberDevice { get; set; }
    }

    /// <summary>Sent by POST api/User/CheckRememberDevice.</summary>
    public class RememberDeviceRequest
    {
        public string PendingUserId { get; set; }
        public string RememberDeviceToken { get; set; }
    }
}