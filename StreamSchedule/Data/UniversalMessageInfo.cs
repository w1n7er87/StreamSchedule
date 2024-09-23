using TwitchLib.Client.Models;

namespace StreamSchedule.Data;

public class UniversalMessageInfo(ChatMessage chatMessage, string commandTrimmedContent, string? replyID, Privileges senderPrivileges)
{
    public string Message = commandTrimmedContent.TrimStart();
    public string Username = chatMessage.Username;
    public string UserId = chatMessage.UserId;
    public string? ReplyID = replyID;
    public Privileges Privileges = senderPrivileges;
}
