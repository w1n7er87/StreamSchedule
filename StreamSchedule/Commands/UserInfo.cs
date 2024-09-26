﻿using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class UserInfo : Command
{
    internal override string Call => "whois";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "user info: [username]";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["f", "e", "s", "a", "g", "c"];

    internal override async Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string text = Commands.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArgs);
        string[] split = text.Split(' ');
        CommandResult response = new("");

        int userIDnumber = 0;
        bool idProvided = false;
        string targetUsername = message.Username;

        if (!string.IsNullOrWhiteSpace(split[0])) // do i have anything provided
        {
            if (split[0].StartsWith('#'))
            {
                idProvided = int.TryParse(split[0].Replace("#", "").Replace("@", ""), out userIDnumber); // if i have numbers with prefix - treat them as userid
            }

            if (!idProvided) { targetUsername = split[0].Replace("#", "").Replace("@", ""); } // if there was no prefix or conversion failed, treat it as a username
        }

        try
        {
            var a = idProvided ? await BotCore.Instance.api.Helix.Users.GetUsersAsync(ids: [userIDnumber.ToString()]) : await BotCore.Instance.api.Helix.Users.GetUsersAsync(logins: [targetUsername]);
            TwitchLib.Api.Helix.Models.Users.GetUsers.User u = a.Users.First();

            string generalInfo = GetGeneralInfo(u);

            if (usedArgs.Count == 0) { return generalInfo; }

            string color = "";
            string followers = "";
            string[] emotes = [];
            string[] liveInfo = [];

            bool all = usedArgs.TryGetValue("a", out _);

            if (usedArgs.TryGetValue("g", out _) || all)
            { response += generalInfo + " "; }

            if (usedArgs.TryGetValue("c", out _) || all)
            {
                color = await GetColor(u.Id);
                response += color + " ";
            }

            if (usedArgs.TryGetValue("f", out _) || all)
            {
                followers = (await GetFollowers(u.Id)).ToString() + " followers";
                response += followers + " ";
            }

            if (usedArgs.TryGetValue("e", out _) || all)
            {
                emotes = await GetEmotes(u.Id);
                response += emotes[1] + " ";
            }

            if (usedArgs.TryGetValue("s", out _) || all)
            {
                liveInfo = await GetLiveStatus(u.Id);
                response += (message.Privileges >= Privileges.Trusted ? liveInfo[1] : liveInfo[0]) + " ";
            }

            if (all)
            { return new($"{generalInfo} | {color} | {followers} | {emotes[0]} | {liveInfo[0]}"); }

            return response;
        }
        catch (Exception ex)
        {
            if (ex is InvalidOperationException)
            {
                return Utils.Responses.Fail + " no user with such name/id";
            }

            Console.WriteLine(ex.ToString());
            return Utils.Responses.Surprise;
        }
    }

    private static async Task<string[]> GetEmotes(string userID)
    {
        try
        {
            var emotes = (await BotCore.Instance.api.Helix.Chat.GetChannelEmotesAsync(userID)).ChannelEmotes;
            string[] result = ["no emotes", "no emotes"];

            if (emotes.Length > 0)
            {
                result[1] = $"{emotes.Length} emotes ({emotes.Count(e => e.Format.Contains("animated"))} animated) ";
                result[0] = result[1];

                int t1 = emotes.Count(e => e.Tier == "1000");
                int t1a = emotes.Count(e => e.Tier == "1000" && e.Format.Contains("animated"));

                int t2 = emotes.Count(e => e.Tier == "2000");
                int t2a = emotes.Count(e => e.Tier == "2000" && e.Format.Contains("animated"));

                int t3 = emotes.Count(e => e.Tier == "3000");
                int t3a = emotes.Count(e => e.Tier == "3000" && e.Format.Contains("animated"));

                int bits = emotes.Count(e => e.EmoteType == "bitstier");
                int bitsa = emotes.Count(e => e.EmoteType == "bitstier" && e.Format.Contains("animated"));

                int follow = emotes.Count(e => e.EmoteType == "follower");
                int followa = emotes.Count(e => e.EmoteType == "follower" && e.Format.Contains("animated"));

                result[1] += $": T1:{t1}({t1a}); T2:{t2}({t2a}); T3:{t3}({t3a}); Bits:{bits}({bitsa}); Follow:{follow}({followa});";
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return [Utils.Responses.Surprise.ToString(), Utils.Responses.Surprise.ToString()];
        }
    }

    private static async Task<string> GetColor(string userID)
    {
        try
        {
            var color = await BotCore.Instance.api.Helix.Chat.GetUserChatColorAsync([userID]);
            return color.Data.First().Color.Equals("") ? "color not set" : color.Data.First().Color;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Utils.Responses.Surprise.ToString();
        }
    }

    private static async Task<string[]> GetLiveStatus(string userID)
    {
        try
        {
            string[] result = ["", ""];

            var liveStatus = await BotCore.Instance.api.Helix.Streams.GetStreamsAsync(userIds: [userID]);

            TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream? s = liveStatus.Streams.FirstOrDefault();
            if (s != null)
            {
                TimeSpan durationSpan = DateTime.Now - s.StartedAt.ToLocalTime();
                string duration = durationSpan.Days > 0 ? durationSpan.ToString("d\\:hh\\:mm\\:ss") : durationSpan.ToString("hh\\:mm\\:ss");
                result = [$"live {s.GameName}", $"Now {s.Type} ({duration}) : {s.GameName} - \" {s.Title} \" for {s.ViewerCount} viewers."];
            }
            else
            {
                result = ["offline", "offline"];
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return [Utils.Responses.Surprise.ToString(), Utils.Responses.Surprise.ToString()];
        }
    }

    private static string GetGeneralInfo(TwitchLib.Api.Helix.Models.Users.GetUsers.User user)
    {
        try
        {
            bool isKnown = true;
            if (!User.TryGetUser("lorem", out User dbData, user.Id))
            {
                isKnown = false;
            }

            string aka = "";

            if (isKnown)
            {
                List<string>? previousUsernames = dbData.PreviousUsernames;
                if (previousUsernames is not null && previousUsernames.Count != 0)
                {
                    aka = "aka: ";
                    foreach (string name in previousUsernames)
                    {
                        aka += name + ", ";
                    }
                    aka = aka[..^2] + ". ";
                }
            }

            return $"{user.Type} {user.BroadcasterType} {user.Login} {aka} (id:{user.Id}) created: {user.CreatedAt:dd/MM/yyyy}";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Utils.Responses.Surprise.ToString();
        }
    }

    private static async Task<int> GetFollowers(string userID) => (await BotCore.Instance.api.Helix.Channels.GetChannelFollowersAsync(userID)).Total;
}
