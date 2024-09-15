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
        int offline = u.MessagesOffline;
        int online = u.MessagesOnline;
        float ratio = (online == 0) ? 1 : MathF.Round(offline / (offline + online), 4);
        float score = MathF.Round((MathF.Log(offline + 1) - MathF.Log10(online + 1)) * ratio, 4);
        return new RatioScore(ratio, score);
    }
}
