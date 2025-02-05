﻿using StreamSchedule.Data;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.Commands;

internal class Lurkers : Command
{
    internal override string Call => "lurkers";

    internal override Privileges MinPrivilege => Privileges.None;

    internal override string Help => "get channel lurkers";

    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int) Cooldowns.FiveMinutes);

    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override string[]? Arguments => null;

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        (int, Chatter?[]?) count = await BotCore.GQLClient.GetChattersCount(message.roomID);
        string chatter = "";
        if (count.Item2 is not null && count.Item2.Length != 0) chatter = $", and @{count.Item2[Random.Shared.Next(count.Item2.Length)]?.Login ?? "StreamSchedule"} is one of them";
        return new($"{count.Item1} lurkers{chatter} uuh");
    }
}
