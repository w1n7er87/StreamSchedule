using StreamSchedule.Data.Models;

namespace StreamSchedule;

public struct RatioScore(float r, float s)
{
    public float ratio = r;
    public float score = s;
}

internal class Userscore
{
    public static RatioScore GetRatioAndScore(User u)
    {
        int offline = (u.MessagesOffline == 0) ? 1 : u.MessagesOffline;
        int online = (u.MessagesOnline == 0) ? 1 : u.MessagesOnline;
        float ratio = offline / (offline + online);
        float score = (MathF.Log(offline) - MathF.Log10(online)) * ratio;
        return new RatioScore(MathF.Round(ratio, 4), MathF.Round(score, 4));
    }
}
