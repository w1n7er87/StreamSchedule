using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace StreamSchedule.Commands;

internal class UserInfo2 : Command
{
    internal override string Call => "whois";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "user info: [username]";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["f", "e", "s", "a", "g", "c", "n", "l"];

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);
        string[] split = text.Split(' ');
        CommandResult response = new() { requiresFilter = true };

        string targetUsername = message.sender.Username!;

        bool idProvided = false;
        int userIDNumber = 0;
        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            if (split[0].StartsWith('#')) idProvided = int.TryParse(split[0].Replace("#", "").Replace("@", ""), out userIDNumber);

            if (!idProvided) { targetUsername = split[0].Replace("#", "").Replace("@", ""); }
        }

        (User?, GetUserErrorReason?) userError;
        if (idProvided) userError = await BotCore.GQLClient.GetUserByID(userIDNumber.ToString());
        else userError = await BotCore.GQLClient.GetUserOrReasonByLogin(targetUsername);

        if (userError.Item1 is null) return new CommandResult("user does not exist");
        User user = userError.Item1;

        string generalInfo = GetGeneralInfo(userError);

        if (usedArgs.Count == 0) { return new(generalInfo, requiresFilter:true); }

        string color = "";
        string followers = "";
        string[] emotes = [];
        string[] liveInfo = [];
        string lurkers = "";

        bool all = usedArgs.TryGetValue("a", out _);

        if (usedArgs.TryGetValue("g", out _) || all)
        { response += generalInfo + " "; }

        if (usedArgs.TryGetValue("n", out _))
        {
            response += PreviousUsernames(user.Id!) + " ";
        }

        if (usedArgs.TryGetValue("c", out _) || all)
        {
            color = await GetColor(user, detailedInfo: !all);
            response += color + " ";
        }

        if (usedArgs.TryGetValue("f", out _) || all)
        {
            followers = (user.Followers?.TotalCount ?? 0) + " followers";
            response += followers + " ";
        }

        if (usedArgs.TryGetValue("e", out _) || all)
        {
            emotes = await GetEmotes(user.Id!);
            response += emotes[1] + " ";
        }

        if (usedArgs.TryGetValue("s", out _) || all)
        {
            liveInfo = GetLiveStatus(user);
            response += (message.sender.Privileges < Privileges.Trusted ? liveInfo[0] : liveInfo[1]) + " ";
        }

        if (usedArgs.TryGetValue("l", out _) || all)
        {
            lurkers = (user!.Channel?.Chatters?.Count) switch { >0 => user.Channel.Chatters.Count + " lurkers", _ => "no lurkers"};
            response += lurkers + " ";
        }

        return all ? new($"{generalInfo} | {color} | {followers} | {lurkers} | {emotes[0]} | {liveInfo[0]}", requiresFilter: true) : response;
    }

    private static async Task<string[]> GetEmotes(string userID)
    {
        var emotes = (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(userID)).ChannelEmotes;
        string[] result = ["no emotes", "no emotes"];

        if (emotes.Length <= 0) return result;

        var firstEmote = await BotCore.GQLClient.GetEmote(emotes[0].Id);
        string prefix = firstEmote?.Token?[..^(firstEmote.Suffix?.Length ?? 0)] ?? "";

        result[1] = $"\"{prefix}\" {emotes.Length} emotes ({emotes.Count(e => e.Format.Contains("animated"))} animated)";
        result[0] = result[1];

        int t1 = emotes.Count(e => e.Tier.Equals("1000"));
        int t1a = emotes.Count(e => e.Tier.Equals("1000") && e.Format.Contains("animated"));
        string T1 = $"{(t1 == 0 ? "" : $"T1: {t1}")}{(t1a == 0 ? "" : $"({t1a})")}";

        int t2 = emotes.Count(e => e.Tier.Equals("2000"));
        int t2a = emotes.Count(e => e.Tier.Equals("2000") && e.Format.Contains("animated"));
        string T2 = $"{(t2 == 0 ? "" : $"T2: {t2}")}{(t2a == 0 ? "" : $"({t2a})")}";

        int t3 = emotes.Count(e => e.Tier.Equals("3000"));
        int t3a = emotes.Count(e => e.Tier.Equals("3000") && e.Format.Contains("animated"));
        string T3 = $"{(t3 == 0 ? "" : $"T3: {t3}")}{(t3a == 0 ? "" : $"({t3a})")}";

        IEnumerable<ChannelEmote> bitEmotes = emotes.Where(e => e.EmoteType.Equals("bitstier"));
        int bits = bitEmotes.Count();

        string B;
        if (bits > 0)
        {
            List<Task<GraphQL.Data.Emote?>> tasks = [];
            tasks.AddRange(bitEmotes.Select(e => BotCore.GQLClient.GetEmote(e.Id)));
            await Task.WhenAll(tasks);

            List<string> bitemotes = [.. tasks.OrderBy(t => t.Result?.BitsBadgeTierSummary?.Threshold ?? 0).Select(emote => $"{emote.Result?.Token ?? ""} - {emote.Result?.BitsBadgeTierSummary?.Threshold ?? 0}")];
            B = $"Bit reward emotes - {bits}: {string.Join(" ", bitemotes)}";
        }
        else
        {
            B = "";
        }

        int follow = emotes.Count(e => e.EmoteType == "follower");
        string F = $"{(follow == 0 ? "" : $"Follow: {follow}")}";

        result[1] += $": {T1} {T2} {T3} {F}. {B}".Trim();
        return result;
    }

    private static async Task<string> GetColor(User user, bool detailedInfo = false)
    {
        string? color = user.ChatColor;
        string? creatorColor = user.PrimaryColorHex;

        if (color is not null)
            color = detailedInfo ? $"{color} {await ColorInfo.GetColor(color)}" : color;
        else
            color = "not set";

        if (creatorColor is not null)
            creatorColor = detailedInfo ? $"#{creatorColor} {await ColorInfo.GetColor(creatorColor)}" : $"#{creatorColor}";
        else
            creatorColor = "not set";

        return $"chat color: {color} accent color: {creatorColor}";
    }

    private static string[] GetLiveStatus(User user)
    {
        string hypeTrain = "";
        if (user.Channel?.HypeTrain?.Execution?.IsActive ?? false)
        {
            float progress = MathF.Round(((float)(user.Channel.HypeTrain.Execution.Progress?.Progression ?? 1.0f) / (user.Channel.HypeTrain.Execution.Progress?.Goal ?? 1.0f)) * 100.0f, 4);
            hypeTrain = $"lvl {user.Channel.HypeTrain.Execution.Progress?.Level?.Value ?? 0} {Helpers.HypeTrainDifficultyToString(user.Channel.HypeTrain.Execution.Config?.Difficulty)} hype train - {progress}%";
        }

        if (user.Stream is null)
        {
            string a;
            if (user.LastBroadcast?.ID is null)
            {
                a = "Never streamed before" + hypeTrain;
            }
            else
            {
                TimeSpan sincePastStream = DateTime.Now - (user.LastBroadcast.StartedAt ?? DateTime.Now);
                a = $"offline, last stream: {user.LastBroadcast.Game?.DisplayName ?? ""} - \" {user.LastBroadcast.Title} \" ({(sincePastStream.Days != 0 ? sincePastStream.Days + "d " : "")}{(sincePastStream.Hours != 0 ? sincePastStream.Hours + "h " : "")}{sincePastStream:m'm 's's '} ago). {hypeTrain}";
            }
            return ["offline", a];
        }

        string mature = user.BroadcastSettings?.IsMature ?? false ? " 🔞" : "";
        TimeSpan durationSpan = DateTime.Now - (user.Stream.CreatedAt?.ToLocalTime() ?? DateTime.MinValue);
        string duration = durationSpan.Days > 0 ? durationSpan.ToString(@"d\:hh\:mm\:ss") : durationSpan.ToString(@"hh\:mm\:ss");
        string viewcount = user.Stream.ViewersCount?.ToString() ?? "";
        string game = user.Stream.Game?.DisplayName ?? "";
        string title = user.BroadcastSettings?.Title ?? "";
        string clips = user.Stream.ClipCount > 0 ? $"{user.Stream.ClipCount} clips" : "";
        string streamStatus = $"({user.Stream.AverageFPS}/{user.Stream.Bitrate}Kbps)";
        return [$"live{mature} {game}", $"live{mature} ({duration}) : {game} - \" {title} \" for {viewcount} viewers.{mature} {streamStatus} {clips} {hypeTrain}"];
    }

    private static string GetGeneralInfo((User?, GetUserErrorReason?) userReason)
    {
        return $"{Helpers.UserErrorReasonToString(userReason.Item2)} {Helpers.UserRolesIsStaff(userReason.Item1!.Roles)} {Helpers.UserRolesIsPartnerOrAffiliate(userReason.Item1!.Roles)} {userReason.Item1.Login} (id:{userReason.Item1.Id}) created: {userReason.Item1.CreatedAt:dd/MMM/yyyy} {(userReason.Item1.DeletedAt is null ? "" : $"deleted: {userReason.Item1.DeletedAt:dd/MMM/yyyy} {userReason.Item1.Channel?.FounderBadgeAvailability switch { (> 0) => $" {userReason.Item1.Channel.FounderBadgeAvailability} founder slots available", _ => "" }}")}";
    }

    private static string PreviousUsernames(string userID)
    {
        if (!Data.Models.User.TryGetUser("this is a bad design" , out Data.Models.User dbData, userID)) return "Unknown user";

        List<string>? previousUsernames = dbData.PreviousUsernames;
        if (previousUsernames is null || previousUsernames.Count == 0) return "Nothing recorded so far";

        return $"aka: {string.Join(", ", previousUsernames)}.";
    }
}
