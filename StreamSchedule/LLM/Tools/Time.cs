namespace StreamSchedule.LLM.Tools;

internal sealed class Time : Tool
{
    public override string Token => "[TIME]";
    public override string Execute() => $"{DateTime.Now:HH:mm:ss}";
}
