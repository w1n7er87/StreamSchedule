using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public record UniversalMessageInfo(User sender, string content, string ID, string? replyID, string channelID, string channelName);
