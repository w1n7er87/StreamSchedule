namespace StreamSchedule.Markov2;

[Flags]
public enum Method
{
    random = 2,
    force = 4,
    ordered = 8,
    reverse = 16,
    weighted = 32,
    include = 64,
}
