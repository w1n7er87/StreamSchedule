using StreamSchedule.Data.Models;

namespace StreamSchedule.Data;

public sealed class Cooldown
{
    private Cooldown() {}

    private const float ResetMultiplier = 2.5f;
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

    public bool Expired => DateTime.Now > expiresAt;
    public TimeSpan currentCooldown => lastCooldown;
    public DateTime ExpiresAtUtc => TimeZoneInfo.ConvertTimeToUtc(expiresAt);
    public DateTime ResetAtUtc => TimeZoneInfo.ConvertTimeToUtc(lastUsedAt + lastCooldown * ResetMultiplier);
    
    public bool TryExtend()
    {
        if (user.Privileges < Privileges.Mod && !Expired)
            return false;

        if (DateTime.Now > lastUsedAt + lastCooldown * ResetMultiplier)
        {
            useCount = 0;
            lastCooldown = baseCooldown;
        }

        lastUsedAt = DateTime.Now;
        lastCooldown += (baseCooldown / 3) * useCount;
        expiresAt = lastUsedAt + lastCooldown;
        useCount++;

        BotCore.Nlog.Info($"{user.Username} c:{useCount} {expiresAt - lastUsedAt} {baseCooldown}");
        return true;
    }
}
