using StreamSchedule.Data.Models;

namespace StreamSchedule;

public struct RatioScore(float r, float s)
{
    public float ratio = r;
    public float score = s;
}

internal static class Userscore
{
    public static RatioScore GetRatioAndScore(User u)
    {
        int offline = u.MessagesOffline;
        int online = u.MessagesOnline;
        int total = offline + online;
        float ratio = 0f;

        if (offline != 0) ratio = (float)offline / total;

        float score = (MathF.Log(MathF.Max(1, offline)) - MathF.Log10(MathF.Max(1, online))) * ratio * (total / 10000.0f);

        return new RatioScore(ratio, score);
    }
}
