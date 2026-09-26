using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Models;
using System;
using System.Threading.Tasks;

namespace SDP1.Hubs
{
    public class VolunteerChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public VolunteerChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinRequestGroup(string requestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"request_{requestId}");
        }

        public async Task LeaveRequestGroup(string requestId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request_{requestId}");
        }

        public async Task SendMessage(int requestId, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText)) return;

            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Session.GetInt32("UserId");
            var role = httpContext?.Session.GetString("UserRole");
            var userName = httpContext?.Session.GetString("UserName") ?? "User";

            if (!userId.HasValue || string.IsNullOrEmpty(role)) return;

            var request = await _context.VolunteerSupportRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return;

            bool isDisabilityRequester = role == "Disability" && request.RequestedByUserId == userId.Value;
            bool isAssignedVolunteer = role == "Volunteer" && request.AssignedVolunteerId == userId.Value;
            bool isAdmin = role == "Admin";

            if (!isDisabilityRequester && !isAssignedVolunteer && !isAdmin)
            {
                return;
            }

            var chatMessage = new VolunteerChatMessage
            {
                VolunteerSupportRequestId = requestId,
                SenderUserId = userId.Value,
                SenderRole = role,
                SenderName = userName,
                MessageText = messageText.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.VolunteerChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Group($"request_{requestId}").SendAsync("ReceiveMessage", new
            {
                id = chatMessage.Id,
                senderUserId = chatMessage.SenderUserId,
                senderRole = chatMessage.SenderRole,
                senderName = chatMessage.SenderName,
                messageText = chatMessage.MessageText,
                sentAt = chatMessage.SentAt.ToString("hh:mm tt")
            });
        }
    }
}
