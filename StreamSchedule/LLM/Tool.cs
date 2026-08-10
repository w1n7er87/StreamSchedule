namespace StreamSchedule.LLM;

internal abstract class Tool
{
    public abstract string Token { get; }
    public abstract string Execute();
}
