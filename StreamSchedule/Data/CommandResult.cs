using System.Text;

namespace StreamSchedule.Data;

internal class CommandResult()
{
    public bool reply = true;
    private readonly StringBuilder _stringBuilder = new();

    public CommandResult(string content, bool reply = true) : this()
    {
        this.reply = reply;
        _stringBuilder = new(content);
    }

    public CommandResult(StringBuilder stringBuilder, bool reply = true) : this()
    {
        this.reply = reply;
        _stringBuilder = stringBuilder;
    }

    public static CommandResult operator +(CommandResult cr, string extra)
    {
        cr._stringBuilder.Append(extra);
        return cr;
    }

    public static CommandResult operator +(CommandResult cr, StringBuilder extra)
    {
        cr._stringBuilder.Append(extra);
        return cr;
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }

    public static implicit operator CommandResult(string s)
    {
        return new CommandResult(s);
    }

    public static implicit operator CommandResult(StringBuilder sb)
    {
        return new CommandResult(sb);
    }
}
