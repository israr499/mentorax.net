using System.ComponentModel.DataAnnotations;

namespace ChatService.Models
{
    public class ChatRoom
    {
        [Key]
        public string RoomID { get; set; } = string.Empty;
        public string StudentID { get; set; } = string.Empty;
        public string TutorID { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ChatMessage
    {
        [Key]
        public string MessageID { get; set; } = string.Empty;
        public string RoomID { get; set; } = string.Empty;
        public string SenderID { get; set; } = string.Empty;
        public string ReceiverID { get; set; } = string.Empty;
        // Added: stores display name so history shows real names, not IDs
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
    public class Student
    {
        public string StudentID { get; set; }
        public string UserID { get; set; }
        public string EducationLevel { get; set; }
        public string SubjectsOfInterest { get; set; }
    }
    public class User
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    public class Tutor
    {
        public string TutorID { get; set; }
        public string UserID { get; set; }
    }

    public class SendMessageDto
    {
        public string RoomID { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ReceiverID { get; set; } = string.Empty;
    }

    public class CreateRoomDto
    {
        public string StudentID { get; set; } = string.Empty;
        public string TutorID { get; set; } = string.Empty;
    }
}
