using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public record UniversalMessageInfo(
    User Sender,
    string Content,
    string ID,
    string? ReplyID,
    string ChannelID,
    string ChannelName);
