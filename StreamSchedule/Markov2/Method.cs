namespace StreamSchedule.Markov2;

[Flags]
public enum Method
{
    random = 2,
    force = 4,
    ordered = 8,
    weighted = 32,
    reverse = 16,
}
