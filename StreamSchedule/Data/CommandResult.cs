using System.Text;

namespace StreamSchedule.Data
{
    internal class CommandResult(string content, bool reply = true)
    {
        public string content = content;
        public bool reply = reply;
        
        public CommandResult() : this(string.Empty, true)
        {
        }

        public static CommandResult operator +(CommandResult cr, string extra)
        {
            cr.content += extra;
            return cr;
        }

        public override string ToString()
        {
            return content;
        }

        public static implicit operator CommandResult(string s)
        {
            return new CommandResult(s, true);
        }

        public static implicit operator CommandResult(StringBuilder sb)
        {
            return new CommandResult(sb.ToString(), true);
        }
    }
}
