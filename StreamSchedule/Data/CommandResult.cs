using System.Text;

namespace StreamSchedule.Data
{
    internal class CommandResult
    {
        private CommandResult() { this.content = string.Empty; this.reply = false; }

        public string content;
        public bool reply;

        public CommandResult(string content)
        {
            this.content = content;
            this.reply = true;
        }

        public CommandResult(string content, bool reply)
        {
            this.content = content;
            this.reply = reply;
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
