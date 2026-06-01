using System.ComponentModel.DataAnnotations;

namespace MentoraXWebApp.Models
{
    // ========== USER MODELS ==========
    public class User
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string ProfileImageUrl { get; set; }

        public string IntroVideoUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string Token { get; set; }
        public bool IsActive { get; set; } = true;

        public string? TwoFactorSecret { get; set; }
        public bool IsTwoFactorEnabled { get; set; } = false;
        public string? LastUsedOTP { get; set; }

        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public bool IsOtpVerified { get; set; }
    }

    public class OtpVerificationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    // OTP Verification Model
    public class OtpVerifyViewModel
    {
        [Required]
        public string Code { get; set; }

        public string? ReturnUrl { get; set; }
    }
    // Two-Factor Authentication Models
    public class TwoFactorSetupViewModel
    {
        public string SecretKey { get; set; }
        public string QrCodeUrl { get; set; }
        public string ManualEntryKey { get; set; }
    }

    public class TwoFactorVerifyViewModel
    {
        [Required]
        public string Code { get; set; }

        public string? ReturnUrl { get; set; }
        public bool RememberMachine { get; set; }
    }

    // Add this class for Remember Token verification result
    public class RememberTokenResult
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
    }
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // ✅ ADD THIS
        public bool RememberDevice { get; set; }
    }
    public class StudentRegisterViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string EducationLevel { get; set; }
        public string SubjectsOfInterest { get; set; }
        public string GradeLevel { get; set; }
    }

    public class TutorRegisterViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Specialization { get; set; }
        public string Qualification { get; set; }
        public decimal ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Bio { get; set; }
        public string TeachingStyle { get; set; }
        public string EducationDetails { get; set; }
        public List<TutorSubjectInput> Subjects { get; set; }
        public List<TutorAvailabilityInput> Availability { get; set; }
        public string? IntroVideoUrl { get; set; }
    }

    public class TutorSubjectInput
    {
        public string SubjectName { get; set; }
        public string GradeLevel { get; set; }
        public decimal ExperienceInSubject { get; set; }
    }

    public class TutorAvailabilityInput
    {
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    // ========== TUTOR MODELS ==========
    public class TutorProfile
    {
        public string TutorID { get; set; }
        public string UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Specialization { get; set; }
        public string Qualification { get; set; }
        public decimal ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Status { get; set; }
        public string Bio { get; set; }
        public string TeachingStyle { get; set; }
        public string ProfileImageUrl { get; set; }
        public List<TutorSubject> Subjects { get; set; }
        public List<TutorAvailability> Availability { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public string? IntroVideoUrl { get; set; }

        public bool IsLegacyUser { get; set; }

        public string? Token { get; set; }
    }

    public class TutorSubject
    {
        public string TutorSubjectID { get; set; }
        public string SubjectName { get; set; }
        public string GradeLevel { get; set; }
        public decimal ExperienceInSubject { get; set; }
    }


    public class TutorAvailability
    {
        public string AvailabilityID { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class TimeSlot
    {
        public string SlotID { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }
    }

    // ========== BOOKING MODELS ==========
    public class Booking
    {
        public string BookingID { get; set; }
        public string StudentID { get; set; }
        public string StudentName { get; set; }
        public string TutorID { get; set; }
        public string TutorName { get; set; }
        public string TutorSpecialization { get; set; }
        public DateTime BookingDate { get; set; }
        public int DurationHours { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string Topic { get; set; }
        public string MeetingLink { get; set; }
        public string ReviewComment { get; set; }
        public int? Rating { get; set; }
    }

    public class BookingRequest
    {
        public string StudentID { get; set; }
        public string TutorID { get; set; }
        public string SlotID { get; set; }
        public DateTime BookingDate { get; set; }
        public int DurationHours { get; set; }
        public string Topic { get; set; }
        public string Notes { get; set; }
    }

    public class ReviewRequest
    {
        public string BookingID { get; set; }
        public string StudentID { get; set; }
        public string TutorID { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    // ========== ADMIN MODELS ==========
    public class AdminDashboardViewModel
    {
        public List<TutorProfile> PendingTutors { get; set; }
        public List<TutorProfile> ApprovedTutors { get; set; }
        public List<Booking> RecentBookings { get; set; }
        public List<User> RecentUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTutors { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    //public class LoginViewModel
    //{
    //    public string Email { get; set; }
    //    public string Password { get; set; }
    //}

    /// <summary>
    /// Matches UserService LoginResponse DTO.
    /// </summary>
    public class LoginApiResponse
    {
        public bool RequiresOtp { get; set; }

        // Present when RequiresOtp = true (OTP flow)
        public string? PendingUserId { get; set; }
        public string? OtpCode { get; set; }   // used to send email from WebApp

        // Present for both cases
        public string? Email { get; set; }

        // Present when RequiresOtp = false (admin direct login)
        public string? Token { get; set; }
        public string? UserID { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// Matches UserService AuthResponse DTO — returned after OTP verification
    /// and CheckRememberDevice.
    /// </summary>
    public class AuthApiResponse
    {
        public string Token { get; set; }
        public string UserID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? RememberDeviceToken { get; set; }
    }

    /// <summary>View model for the OTP entry form.</summary>
    public class VerifyOtpViewModel
    {
        public string PendingUserId { get; set; }
        public string MaskedEmail { get; set; }
        public string OtpCode { get; set; }
    }

}