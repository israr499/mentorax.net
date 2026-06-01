using System.ComponentModel.DataAnnotations;

namespace BookingService.Models
{
    public class Booking
    {
        [Key]
        public string BookingID { get; set; }

        [Required]
        public string StudentID { get; set; }

        [Required]
        public string TutorID { get; set; }

        public string? SlotID { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        public int DurationHours { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public string? Topic { get; set; }

        public string? Notes { get; set; }

        public string? MeetingLink { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Review
    {
        [Key]
        public string ReviewID { get; set; }
        public string BookingID { get; set; }
        public string StudentID { get; set; }
        public string TutorID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    // DTO for receiving review from client (without ReviewID)
    public class ReviewRequest
    {
        public string BookingID { get; set; }
        public string StudentID { get; set; }
        public string TutorID { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
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

    public class BookingWithDetails
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
        public string? ReviewComment { get; set; }
        public int? Rating { get; set; }
    }
}