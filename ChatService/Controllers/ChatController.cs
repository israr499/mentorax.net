using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatService.Data;
using ChatService.Models;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatDbContext _context;

        public ChatController(ChatDbContext context)
        {
            _context = context;
        }

        // Helper method to get UserID from StudentID or TutorID
        private async Task<string> GetUserIdFromAnyId(string id)
        {
            // Check if it's a UserID
            var user = await _context.Users.FindAsync(id);
            if (user != null) return id;

            // Check if it's a StudentID
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentID == id);
            if (student != null) return student.UserID;

            // Check if it's a TutorID
            var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.TutorID == id);
            if (tutor != null) return tutor.UserID;

            return id;
        }

        [HttpGet("GetOrCreateRoom")]
        public async Task<ActionResult<object>> GetOrCreateRoom(string studentId, string tutorId)
        {
            // Convert both IDs to actual UserIDs
            var actualStudentId = await GetUserIdFromAnyId(studentId);
            var actualTutorId = await GetUserIdFromAnyId(tutorId);

            Console.WriteLine($"GetOrCreateRoom: studentId={studentId} -> {actualStudentId}, tutorId={tutorId} -> {actualTutorId}");

            var room = await _context.ChatRooms
                .FirstOrDefaultAsync(r =>
                    (r.StudentID == actualStudentId && r.TutorID == actualTutorId) ||
                    (r.StudentID == actualTutorId && r.TutorID == actualStudentId));

            if (room == null)
            {
                room = new ChatRoom
                {
                    RoomID = Guid.NewGuid().ToString(),
                    StudentID = actualStudentId,
                    TutorID = actualTutorId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatRooms.Add(room);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Created new room: {room.RoomID}");
            }

            return Ok(new
            {
                room.RoomID,
                StudentID = room.StudentID,
                TutorID = room.TutorID,
                room.CreatedAt
            });
        }

        [HttpGet("GetUserRooms/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserRooms(string userId)
        {
            // Convert to actual UserID if needed
            var actualUserId = await GetUserIdFromAnyId(userId);

            var rooms = await _context.ChatRooms
                .Where(r => r.StudentID == actualUserId || r.TutorID == actualUserId)
                .ToListAsync();

            var result = new List<object>();
            foreach (var room in rooms)
            {
                var unreadCount = await _context.ChatMessages
                    .CountAsync(m => m.RoomID == room.RoomID && m.ReceiverID == actualUserId && !m.IsRead);

                result.Add(new
                {
                    room.RoomID,
                    StudentID = room.StudentID,
                    TutorID = room.TutorID,
                    UnreadCount = unreadCount
                });
            }

            return Ok(result);
        }

        [HttpGet("GetMessages/{roomId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetMessages(string roomId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.RoomID == roomId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageID,
                    m.RoomID,
                    m.SenderID,
                    m.SenderName,
                    m.ReceiverID,
                    m.Message,
                    m.IsRead,
                    m.SentAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("GetUnreadCount/{userId}")]
        public async Task<ActionResult<object>> GetUnreadCount(string userId)
        {
            var actualUserId = await GetUserIdFromAnyId(userId);

            var totalUnread = await _context.ChatMessages
                .CountAsync(m => m.ReceiverID == actualUserId && !m.IsRead);

            return Ok(new { unreadCount = totalUnread });
        }
    }
}