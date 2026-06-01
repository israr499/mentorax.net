namespace BookingService.DTOs
{
    public class TutorRevenueDto
    {
        public string TutorID { get; set; }
        public decimal GrossRevenue { get; set; }         // 100% of all completed bookings
        public decimal PlatformFee { get; set; }          // 30%
        public decimal TotalEarned { get; set; }          // 70% (tutor's share)
        public int TotalBookings { get; set; }
        public decimal TotalHours { get; set; }
        public List<MonthlyRevenueDto> MonthlyBreakdown { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; }       // "2026-01"
        public decimal Amount { get; set; }     // tutor's 70%
        public int Bookings { get; set; }
        public decimal Hours { get; set; }
    }

    public class AdminRevenueSummaryDto
    {
        public decimal TotalGrossRevenue { get; set; }
        public decimal TotalPlatformCommission { get; set; }  // 30%
        public decimal TotalTutorPayouts { get; set; }        // 70%
        public int TotalBookings { get; set; }
        public decimal TotalHours { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<AdminBookingLineDto> Bookings { get; set; } = new();
    }

    public class AdminBookingLineDto
    {
        public string BookingID { get; set; }
        public string StudentID { get; set; }
        public string TutorID { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal DurationHours { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal TutorPayout { get; set; }
        public string Status { get; set; }
        public string Topic { get; set; }
    }
}
