using Microsoft.AspNetCore.SignalR;
using ChatService.Data;
using ChatService.Models;

namespace ChatService.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatDbContext _context;

        public ChatHub(ChatDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                Console.WriteLine($"✅ User {userId} connected: {Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string roomId, string senderUserId, string message)
        {
            try
            {
                Console.WriteLine($"=== SEND MESSAGE === Room:{roomId} Sender:{senderUserId}");

                // Get sender name BEFORE saving
                var user = await _context.Users.FindAsync(senderUserId);
                var senderName = user?.FullName ?? "User";

                var chatMessage = new ChatMessage
                {
                    MessageID = Guid.NewGuid().ToString(),
                    RoomID = roomId,
                    SenderID = senderUserId,
                    SenderName = senderName,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                var response = new
                {
                    messageId = chatMessage.MessageID,
                    roomId = roomId,
                    senderUserId = senderUserId,
                    senderName = senderName,
                    message = message,
                    sentAt = chatMessage.SentAt
                };

                await Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", response);
                Console.WriteLine($"✅ Broadcast to room_{roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SendMessage ERROR: {ex.Message}");
                throw;
            }
        }

        // string roomId — not Guid — so JS can pass the raw string directly
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
            Console.WriteLine($"➕ {Context.ConnectionId} joined room_{roomId}");
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
        }

        public async Task MarkAsRead(string roomId, string userId)
        {
            var messages = _context.ChatMessages
                .Where(m => m.RoomID == roomId && m.ReceiverID == userId && !m.IsRead)
                .ToList();
            foreach (var msg in messages) msg.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task Typing(string roomId, string userName, bool isTyping)
        {
            await Clients.OthersInGroup($"room_{roomId}")
                         .SendAsync("UserTyping", new { roomId, userName, isTyping });
        }
    }
}
