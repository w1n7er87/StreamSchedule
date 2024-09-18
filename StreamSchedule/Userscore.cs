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
        int offline = Math.Max(1, u.MessagesOffline);
        int online = Math.Max(1, u.MessagesOnline);
        float ratio = (float)offline / (offline + online);
        float score = (MathF.Log((float)offline) - MathF.Log10((float)online)) * ratio;

        return new RatioScore(ratio, score);
    }
}
