using StreamSchedule.Data;
using StreamSchedule.GraphQL;
using StreamSchedule.GraphQL.Data;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using Emote = StreamSchedule.GraphQL.Data.Emote;

namespace StreamSchedule.Commands;

internal class UserInfo2 : Command
{
    public override string Call => "whois";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "user info: [username]";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["f", "e", "s", "a", "g", "c", "n", "l", "h"];
    public override List<string> Aliases { get; set; } = [];

    public override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArgs);
        string[] split = text.Split(' ');
        CommandResult response = new() { requiresFilter = true };

        string targetUsername = message.Sender.Username!;

        bool idProvided = false;
        int userIDNumber = 0;
        if (!string.IsNullOrWhiteSpace(split[0]))
        {
            if (split[0].StartsWith('#'))
                idProvided = int.TryParse(split[0].Replace("#", "").Replace("@", ""), out userIDNumber);

            if (!idProvided) targetUsername = split[0].Replace("#", "").Replace("@", "");
        }

        GetUserResult userErrorAndNameAvailable;
        if (idProvided) userErrorAndNameAvailable = await GraphQLClient.GetUserByID(userIDNumber.ToString());
        else userErrorAndNameAvailable = await GraphQLClient.GetUserOrReasonByLogin(targetUsername);

        string usernameAvailable = userErrorAndNameAvailable.IsUsernameAvailable ?? false ? " yet" : "";
        if (userErrorAndNameAvailable.User is null) return new($"user does not exist{usernameAvailable}");
        User user = userErrorAndNameAvailable.User;

        string generalInfo = GetGeneralInfo(userErrorAndNameAvailable);

        if (usedArgs.Count == 0) return new(generalInfo, requiresFilter: true);

        string color = "";
        string followers = "";
        string[] emotes = [];
        string[] liveInfo = [];
        string lurkers = "";
        string hypeTrain = "";

        bool all = usedArgs.TryGetValue("a", out _);

        if (usedArgs.TryGetValue("g", out _) || all) response += generalInfo + " ";

        if (usedArgs.TryGetValue("n", out _)) response += PreviousUsernames(user.Id!) + " ";

        if (usedArgs.TryGetValue("c", out _) || all)
        {
            color = await GetColor(user, !all);
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
            response += (message.Sender.Privileges < Privileges.Trusted ? liveInfo[0] : liveInfo[1]) + " ";
        }

        if (usedArgs.TryGetValue("l", out _) || all)
        {
            lurkers = user!.Channel?.Chatters?.Count switch
            {
                > 0 => user.Channel.Chatters.Count + " lurkers", 
                _ => "no lurkers"
            };
            response += lurkers + " ";
        }

        if (usedArgs.TryGetValue("h", out _) || all)
        {
            hypeTrain = GetHypeTran(user);
            response += hypeTrain + " ";
        }

        return all
            ? new($"{generalInfo} | {color} | {followers} | {lurkers} | {emotes[0]} | {liveInfo[0]} | {hypeTrain}", requiresFilter: true)
            : response;
    }

    private static async Task<string[]> GetEmotes(string userID)
    {
        ChannelEmote[]? emotes = (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(userID)).ChannelEmotes;
        string[] result = ["no emotes", "no emotes"];

        if (emotes.Length <= 0) return result;

        Emote? firstEmote = await GraphQLClient.GetEmote(emotes[0].Id);
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
            List<Task<Emote?>> tasks = [];
            tasks.AddRange(bitEmotes.Select(e => GraphQLClient.GetEmote(e.Id)));
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
            creatorColor = detailedInfo
                ? $"#{creatorColor} {await ColorInfo.GetColor(creatorColor)}"
                : $"#{creatorColor}";
        else
            creatorColor = "not set";

        return $"chat color: {color}, accent color: {creatorColor}";
    }

    private static string[] GetLiveStatus(User user)
    {
        string hypeTrain = "";
        if (user.Channel?.HypeTrain?.Execution is not null)
        {
            float progress = GetPercentage(user.Channel.HypeTrain.Execution.Progress?.Progression, user.Channel.HypeTrain.Execution.Progress?.Goal);
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
                TimeSpan sincePastStream = user.LastBroadcast.StartedAt is null
                    ? TimeSpan.Zero
                    : DateTime.Now - TimeZoneInfo.ConvertTimeFromUtc((DateTime)user.LastBroadcast.StartedAt, TimeZoneInfo.Local);
                a = $"offline, last stream: {user.LastBroadcast.Game?.DisplayName ?? ""} - \" {user.LastBroadcast.Title} \" ({(sincePastStream.Days != 0 ? sincePastStream.Days + "d " : "")}{(sincePastStream.Hours != 0 ? sincePastStream.Hours + "h " : "")}{sincePastStream:m'm 's's '} ago). {hypeTrain}";
            }
            return ["offline", a];
        }

        string mature = user.BroadcastSettings?.IsMature ?? false ? " 🔞" : "";
        TimeSpan durationSpan = DateTime.Now - (user.Stream.CreatedAt?.ToLocalTime() ?? DateTime.MinValue);
        string duration = durationSpan.Days > 0
            ? durationSpan.ToString(@"d\:hh\:mm\:ss")
            : durationSpan.ToString(@"hh\:mm\:ss");
        string viewcount = user.Stream.ViewersCount?.ToString() ?? "";
        string game = user.Stream.Game?.DisplayName ?? "";
        string title = user.BroadcastSettings?.Title ?? "";
        string clips = user.Stream.ClipCount > 0 ? $"{user.Stream.ClipCount} clips" : "";
        string streamStatus = $"({user.Stream.AverageFPS}/{user.Stream.Bitrate}Kbps)";
        return [$"live{mature} {game}", $"live{mature} ({duration}) : {game} - \" {title} \" for {viewcount} viewers.{mature} {streamStatus} {clips} {hypeTrain}"];
    }

    private static string GetGeneralInfo(GetUserResult userReason)
    {
        return $"{Helpers.UserErrorReasonToString(userReason.Reason)} " +
               $"{Helpers.UserRolesToLongString(userReason.User?.Roles)} " +
               $"{userReason.User?.Login} " +
               $"(id:{userReason.User?.Id}) " +
               $"created: {userReason.User?.CreatedAt:dd/MMM/yyyy} " +
               $"{(userReason.User?.DeletedAt is null ? "" : $"deleted: {userReason.User.DeletedAt:dd/MMM/yyyy}")} " +
               $"{userReason.User?.Channel?.FounderBadgeAvailability switch { > 0 => $" {userReason.User.Channel.FounderBadgeAvailability} founder slots available", _ => "" }} " +
               $"updated: {userReason.User?.UpdatedAt:dd/MMM/yyyy} ";
    }

    private static string PreviousUsernames(string userID)
    {
        if (!Data.Models.User.TryGetUser("", out Data.Models.User dbData, userID)) return "Unknown user";

        List<string>? previousUsernames = dbData.PreviousUsernames;
        if (previousUsernames is null || previousUsernames.Count == 0) return "Nothing recorded so far";

        return $"aka: {string.Join(", ", previousUsernames)}.";
    }

    private static string GetHypeTran(User user)
    {
        if (user.Channel?.HypeTrain?.Approaching is not null)
        {
            string events = user.Channel.HypeTrain.Approaching.EventsRemaining?.FirstOrDefault()?.Events.ToString() ??
                            "0";
            string isKappaApproaching = user.Channel.HypeTrain.Approaching.IsGoldenKappaTrain ?? false ? "golden Kappa" : "";
            string isTreasureApproaching = user.Channel.HypeTrain.Approaching.IsTreasureTrain ?? false ? "treasure" : "";
            string secondsLeft = ((user.Channel.HypeTrain.Approaching.ExpiresAt ?? DateTime.UtcNow) - DateTime.UtcNow).Seconds.ToString();
            return $"{isKappaApproaching} {isTreasureApproaching} hype train is approaching ({events} more events) {secondsLeft}s";
        }

        if (user.Channel?.HypeTrain?.Execution is null) return "no active hype trains";

        string isKappa = user.Channel.HypeTrain.Execution.IsGoldenKappaTrain ?? false ? "golden Kappa " : "";
        string isTreasure = user.Channel.HypeTrain.Execution.IsTreasureTrain ?? false ? "treasure " : "";

        float progress = GetPercentage(user.Channel.HypeTrain.Execution.Progress?.Progression, user.Channel.HypeTrain.Execution.Progress?.Goal);
        string hypeTrain = $"lvl {user.Channel.HypeTrain.Execution.Progress?.Level?.Value ?? 0} {Helpers.HypeTrainDifficultyToString(user.Channel.HypeTrain.Execution.Config?.Difficulty)} {isTreasure}{isKappa}hype train - {progress}%";
        hypeTrain += $" ( record: lvl {user.Channel.HypeTrain.Execution.AllTimeHigh?.Level?.Value ?? 0} {GetPercentage(user.Channel.HypeTrain.Execution.AllTimeHigh?.Progression, user.Channel.HypeTrain.Execution.AllTimeHigh?.Goal)}% ) ";

        string shared = "";
        if (user.Channel.HypeTrain.Execution.SharedHypeTrainDetails is not null)
        {
            shared = "shared contribution: ";
            shared += string.Join(", ", user.Channel.HypeTrain.Execution.SharedHypeTrainDetails.SharedProgress?.Select(x => $"{x?.User?.Login} - {GetPercentage(x?.ChannelProgress?.Total, user.Channel.HypeTrain.Execution.Progress?.Total)}%") ?? []);
            shared += $". ( shared record: lvl {user.Channel.HypeTrain.Execution.SharedHypeTrainDetails.SharedAllTimeHighRecords?[0]?.ChannelAllTimeHigh?.Level?.Value ?? 0} {GetPercentage(user.Channel.HypeTrain.Execution.SharedHypeTrainDetails.SharedAllTimeHighRecords?[0]?.ChannelAllTimeHigh?.Progression, user.Channel.HypeTrain.Execution.SharedHypeTrainDetails.SharedAllTimeHighRecords?[0]?.ChannelAllTimeHigh?.Goal)}% ) ";
        }

        string treasureDetails = "";
        if (user.Channel.HypeTrain.Execution.TreasureTrainDetails is not null)
            treasureDetails = $" ( discount {user.Channel.HypeTrain.Execution.TreasureTrainDetails.DiscountPercentage}%, starts at lvl{user.Channel.HypeTrain.Execution.TreasureTrainDetails.DiscountLevelThreshold} ) ";

        hypeTrain += treasureDetails;
        hypeTrain += shared;
        hypeTrain += string.Join("; ", user.Channel.HypeTrain.Execution.Participations?.Select(x => x?.ToString() ?? "") ?? []);
        return hypeTrain;
        
    }
    private static float GetPercentage(int? currentProgress, int? goal) => MathF.Round((currentProgress ?? 1.0f) / (goal ?? 1.0f) * 100.0f, 4);
}
