namespace StreamSchedule.Data;

internal class OutgoingMessage
{
    public CommandResult Result { get; } = null!;
    public string? ReplyID { get; set; } = null;

    private OutgoingMessage() { }

    private OutgoingMessage(CommandResult result)
    {
        Result = result;
    }

    public OutgoingMessage(CommandResult result, string? replyID) : this(result)
    {
        ReplyID = result.reply ? replyID : null;
    }

    public static implicit operator OutgoingMessage(CommandResult result) => new(result);
}
