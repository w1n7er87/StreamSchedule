namespace StreamSchedule.LLM.Tools;

internal sealed class Spam : Tool
{
    public override string Token => "[SPM]";
    public override string Execute() => "...";
}
