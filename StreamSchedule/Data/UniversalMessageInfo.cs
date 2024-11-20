using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public record UniversalMessageInfo(User sender, string content, string? replyID, string roomID);
