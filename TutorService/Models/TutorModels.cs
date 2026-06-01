using System.ComponentModel.DataAnnotations;

namespace TutorService.Models
{
    public class Tutor
    {
        [Key]
        public string TutorID { get; set; }

        [Required]
        public string UserID { get; set; }

        public string? Specialization { get; set; }

        public string? Qualification { get; set; }

        public decimal ExperienceYears { get; set; }

        public decimal HourlyRate { get; set; }

        public string Status { get; set; } = "Pending";

        public string? Bio { get; set; }

        public string? TeachingStyle { get; set; }

        public string? EducationDetails { get; set; }

        public string? AdminApprovedBy { get; set; }

        public DateTime? AdminApprovedAt { get; set; }

        public string? RejectionReason { get; set; }
        public string? IntroVideoUrl { get; set; }  // ← ADD THIS

    }

    public class TutorSubject
    {
        [Key]
        public string TutorSubjectID { get; set; }
        public string TutorID { get; set; }
        public string SubjectName { get; set; }
        public string? GradeLevel { get; set; }
        public decimal ExperienceInSubject { get; set; }
    }

    public class TutorAvailability
    {
        [Key]
        public string AvailabilityID { get; set; }
        public string TutorID { get; set; }
        public int DayOfWeek { get; set; } // 0=Sunday, 1=Monday, etc.
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsRecurring { get; set; } = true;
    }

    public class TimeSlot
    {
        [Key]
        public string SlotID { get; set; }
        public string TutorID { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; } = false;
        public string? BookingID { get; set; }
    }

    public class TutorProfileRequest
    {
        public string UserID { get; set; }
        public string Specialization { get; set; }
        public string Qualification { get; set; }
        public decimal ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Bio { get; set; }
        public string TeachingStyle { get; set; }
        public string EducationDetails { get; set; }
        public List<TutorSubjectRequest> Subjects { get; set; }
        public List<TutorAvailabilityRequest> Availability { get; set; }
        public string? IntroVideoUrl { get; set; }
    }

    public class TutorSubjectRequest
    {
        public string SubjectName { get; set; }
        public string GradeLevel { get; set; }
        public decimal ExperienceInSubject { get; set; }
    }

    public class TutorAvailabilityRequest
    {
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class TutorApprovalRequest
    {
        public string TutorID { get; set; }
        public string AdminID { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class TutorWithUserInfo
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
        public string? IntroVideoUrl { get; set; }
    }
}