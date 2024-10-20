using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Ping : Command
{
    internal override string Call => "ping";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "ping";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Minute);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string channels = "";
        foreach (var item in BotCore.Instance.Client.JoinedChannels)
        {
            channels += item.Channel + ", ";
        }

        Console.WriteLine($"is connected: {BotCore.Instance.Client.IsConnected} joined channels: {channels} \n");
        BotCore.Instance.Client.Reconnect();
        return Task.FromResult(new CommandResult("PotFriend ", false));
    }
}
