using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public sealed class Cooldown
{
    private Cooldown() {}

    private readonly TimeSpan baseCooldown;
    private readonly User user;

    private TimeSpan lastCooldown;
    private DateTime lastUsedAt;
    private DateTime expiresAt;
    private int useCount = 0;

    public Cooldown(User u, TimeSpan BaseCooldown)
    {
        user = u;
        lastUsedAt = DateTime.Now;
        baseCooldown = BaseCooldown;
        expiresAt = DateTime.Now + BaseCooldown;
        lastCooldown = baseCooldown;
    }

    public bool TryExtend()
    {
        if (user.Privileges < Privileges.Mod && DateTime.Now < expiresAt)
            return false;

        if (DateTime.Now > lastUsedAt + (lastCooldown * 3))
            useCount = 0;

        lastUsedAt = DateTime.Now;
        lastCooldown += (baseCooldown / 3) * useCount;
        expiresAt = lastUsedAt + lastCooldown;
        useCount++;

        BotCore.Nlog.Info($"{user.Username} c:{useCount} {expiresAt - lastUsedAt} {baseCooldown}");
        return true;
    }
}
