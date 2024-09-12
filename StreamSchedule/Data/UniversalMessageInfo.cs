using TwitchLib.Client.Models;

namespace StreamSchedule.Data;

public class UniversalMessageInfo
{
    public string Message;
    public string Username;
    public string UserId;
    public string? ReplyID;
    public Privileges Privileges;

    public UniversalMessageInfo(ChatMessage chatMessage, string commandTrimmedContent, string? replyID, Privileges senderPrivileges)
    {
        Message = commandTrimmedContent.Length > 1 && commandTrimmedContent[0].Equals(' ') ? commandTrimmedContent[1..] : commandTrimmedContent;
        Username = chatMessage.Username;
        UserId = chatMessage.UserId;
        ReplyID = replyID;
        Privileges = senderPrivileges;
    }
}
