using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.DTOs;
using System.Text;

namespace BookingService.Controllers
{
    /// <summary>
    /// Revenue reporting endpoints.
    /// Tutor share = 70%   Platform share = 30%
    /// Only Completed bookings count as revenue.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RevenueController : ControllerBase
    {
        private const decimal TUTOR_SHARE = 0.70m;
        private const decimal PLATFORM_SHARE = 0.30m;

        private readonly BookingDbContext _context;
        private readonly ILogger<RevenueController> _logger;

        public RevenueController(BookingDbContext context, ILogger<RevenueController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // GET api/Revenue/tutor/{tutorId}
        // Returns revenue summary + 12-month breakdown for a tutor.
        // ─────────────────────────────────────────────────────────────
        [HttpGet("tutor/{tutorId}")]
        public async Task<IActionResult> GetTutorRevenue(string tutorId)
        {
            if (string.IsNullOrWhiteSpace(tutorId))
                return BadRequest(new { success = false, message = "TutorID is required." });

            try
            {
                var completedBookings = await _context.Bookings
                    .Where(b => b.TutorID == tutorId && b.Status == "Completed")
                    .ToListAsync();

                var grossRevenue   = completedBookings.Sum(b => b.TotalAmount);
                var platformFee    = Math.Round(grossRevenue * PLATFORM_SHARE, 2);
                var totalEarned    = Math.Round(grossRevenue * TUTOR_SHARE, 2);
                var totalHours     = completedBookings.Sum(b => (decimal)b.DurationHours);

                // Build last-12-months breakdown
                var cutoff = DateTime.UtcNow.AddMonths(-11);
                cutoff = new DateTime(cutoff.Year, cutoff.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var monthly = completedBookings
                    .Where(b => b.BookingDate >= cutoff)
                    .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                    .Select(g => new MonthlyRevenueDto
                    {
                        Month    = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        Amount   = Math.Round(g.Sum(b => b.TotalAmount) * TUTOR_SHARE, 2),
                        Bookings = g.Count(),
                        Hours    = g.Sum(b => (decimal)b.DurationHours)
                    })
                    .OrderBy(m => m.Month)
                    .ToList();

                var dto = new TutorRevenueDto
                {
                    TutorID          = tutorId,
                    GrossRevenue     = grossRevenue,
                    PlatformFee      = platformFee,
                    TotalEarned      = totalEarned,
                    TotalBookings    = completedBookings.Count,
                    TotalHours       = totalHours,
                    MonthlyBreakdown = monthly
                };

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tutor revenue for {TutorId}", tutorId);
                return StatusCode(500, new { success = false, message = "Error fetching revenue data." });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET api/Revenue/admin/summary?fromDate=&toDate=
        // ─────────────────────────────────────────────────────────────
        [HttpGet("admin/summary")]
        public async Task<IActionResult> GetAdminSummary(
            [FromQuery] string fromDate = "",
            [FromQuery] string toDate   = "")
        {
            try
            {
                DateTime from = string.IsNullOrWhiteSpace(fromDate)
                    ? DateTime.UtcNow.AddMonths(-1)
                    : DateTime.Parse(fromDate).ToUniversalTime();

                DateTime to = string.IsNullOrWhiteSpace(toDate)
                    ? DateTime.UtcNow
                    : DateTime.Parse(toDate).ToUniversalTime().AddDays(1).AddSeconds(-1);

                var bookings = await _context.Bookings
                    .Where(b => b.Status == "Completed"
                             && b.BookingDate >= from
                             && b.BookingDate <= to)
                    .ToListAsync();

                var grossRevenue  = bookings.Sum(b => b.TotalAmount);
                var commission    = Math.Round(grossRevenue * PLATFORM_SHARE, 2);
                var tutorPayouts  = Math.Round(grossRevenue * TUTOR_SHARE, 2);

                var lines = bookings.Select(b => new AdminBookingLineDto
                {
                    BookingID    = b.BookingID,
                    StudentID    = b.StudentID,
                    TutorID      = b.TutorID,
                    BookingDate  = b.BookingDate,
                    DurationHours = b.DurationHours,
                    TotalAmount  = b.TotalAmount,
                    PlatformFee  = Math.Round(b.TotalAmount * PLATFORM_SHARE, 2),
                    TutorPayout  = Math.Round(b.TotalAmount * TUTOR_SHARE, 2),
                    Status       = b.Status,
                    Topic        = b.Topic ?? ""
                }).OrderByDescending(b => b.BookingDate).ToList();

                var dto = new AdminRevenueSummaryDto
                {
                    TotalGrossRevenue       = grossRevenue,
                    TotalPlatformCommission = commission,
                    TotalTutorPayouts       = tutorPayouts,
                    TotalBookings           = bookings.Count,
                    TotalHours              = bookings.Sum(b => (decimal)b.DurationHours),
                    FromDate                = from,
                    ToDate                  = to,
                    Bookings                = lines
                };

                return Ok(new { success = true, data = dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin revenue summary");
                return StatusCode(500, new { success = false, message = "Error fetching revenue summary." });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET api/Revenue/admin/csv-report?fromDate=&toDate=
        // Returns a CSV file download — no external packages needed.
        // ─────────────────────────────────────────────────────────────
        [HttpGet("admin/csv-report")]
        public async Task<IActionResult> DownloadCsvReport(
            [FromQuery] string fromDate = "",
            [FromQuery] string toDate   = "")
        {
            try
            {
                DateTime from = string.IsNullOrWhiteSpace(fromDate)
                    ? DateTime.UtcNow.AddMonths(-1)
                    : DateTime.Parse(fromDate).ToUniversalTime();

                DateTime to = string.IsNullOrWhiteSpace(toDate)
                    ? DateTime.UtcNow
                    : DateTime.Parse(toDate).ToUniversalTime().AddDays(1).AddSeconds(-1);

                var bookings = await _context.Bookings
                    .Where(b => b.Status == "Completed"
                             && b.BookingDate >= from
                             && b.BookingDate <= to)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine("BookingID,TutorID,StudentID,BookingDate,DurationHours,GrossAmount,PlatformFee(30%),TutorPayout(70%),Topic,Status");

                foreach (var b in bookings)
                {
                    var fee    = Math.Round(b.TotalAmount * PLATFORM_SHARE, 2);
                    var payout = Math.Round(b.TotalAmount * TUTOR_SHARE, 2);
                    sb.AppendLine($"{b.BookingID},{b.TutorID},{b.StudentID},{b.BookingDate:yyyy-MM-dd HH:mm},{b.DurationHours},{b.TotalAmount:F2},{fee:F2},{payout:F2},\"{b.Topic?.Replace("\"","'")}\",{b.Status}");
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var fileName = $"MentoraX_Revenue_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV report");
                return StatusCode(500, new { success = false, message = "Error generating report." });
            }
        }
    }
}
