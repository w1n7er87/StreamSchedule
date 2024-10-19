using StreamSchedule.Data.Models;
namespace StreamSchedule.Data;

public class UniversalMessageInfo(User sender, string commandTrimmedContent, string? replyID)
{
    public string Message = commandTrimmedContent.TrimStart();
    public User Sender = sender;
    public string? ReplyID = replyID;
}
