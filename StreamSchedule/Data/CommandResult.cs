using System.Text;

namespace StreamSchedule.Data;

internal class CommandResult
{
    public readonly bool reply = true;
    public bool requiresFilter = false;
    private readonly StringBuilder _stringBuilder;

    public CommandResult()
    {
        _stringBuilder = new();
    }

    public CommandResult(string content, bool reply = true, bool requiresFilter = false)
    {
        this.reply = reply;
        this.requiresFilter = requiresFilter;
        _stringBuilder = new(content);
    }

    public CommandResult(StringBuilder stringBuilder, bool reply = true, bool requiresFilter = false)
    {
        this.reply = reply;
        this.requiresFilter = requiresFilter;
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

    public override string ToString() => _stringBuilder.ToString();
    public static implicit operator CommandResult(string s) => new(s);
    public static implicit operator CommandResult(StringBuilder sb) => new(sb);

}
