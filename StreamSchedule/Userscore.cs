using StreamSchedule.Data.Models;

namespace StreamSchedule;

public readonly struct RatioScore(float r, float s)
{
    public readonly float ratio = r;
    public readonly float score = s;
}

internal static class Userscore
{
    public static RatioScore GetRatioAndScore(User u)
    {
        int offline = u.MessagesOffline;
        int online = u.MessagesOnline;

        int offlineSafe = Math.Max(1, offline);
        int onlineSafe = Math.Max(1, online);

        int total = offline + online;
        float ratio = 0f;

        if (offline != 0) ratio = (float)offline / total;

        float score = MathF.Log10(0.01f * offlineSafe + 5f * onlineSafe) - MathF.Log10(onlineSafe / MathF.Max(1, 0.5f * total));

        return new RatioScore(ratio, score);
    }
}
