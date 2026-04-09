using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public sealed class Cooldown
{
    private Cooldown() {}

    private readonly TimeSpan baseCooldown;
    private readonly User user;

    private DateTime lastUsedAt;
    private DateTime expiresAt;
    private int useCount = 0;

    public Cooldown(User u, TimeSpan BaseCooldown)
    {
        user = u;
        lastUsedAt = DateTime.Now;
        baseCooldown = BaseCooldown;
        expiresAt = DateTime.Now + BaseCooldown;
    }

    public bool TryExtend()
    {
        if (user.Privileges < Privileges.Mod && DateTime.Now < expiresAt)
            return false;

        if (DateTime.Now > lastUsedAt + (baseCooldown * 3))
            useCount = 0;

        lastUsedAt = DateTime.Now;
        expiresAt = lastUsedAt + baseCooldown + (baseCooldown / 3) * useCount;
        useCount++;

        BotCore.Nlog.Info($"{user.Username} c:{useCount} {expiresAt - lastUsedAt} {baseCooldown}");
        return true;
    }
}
