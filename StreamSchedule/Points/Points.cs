using NeoSmart.Unicode;

namespace StreamSchedule.Points;

public static class Points
{
    public static long ScoreMessage(string message)
    {
        long score = (long)Math.Ceiling(message.Length / 500f * message.Codepoints().Distinct().Count());
        Console.WriteLine($"{message.Length} - {score} - {message}");
        return score;
    }
}
