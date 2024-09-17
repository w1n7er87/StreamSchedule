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
        string text = Utils.RetrieveArguments(Arguments!, message.Message, out List<string> usedArgs);
        Console.WriteLine(text);
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
            var a = idProvided ? await Body.main.api.Helix.Users.GetUsersAsync(ids: [userIDnumber.ToString()]) : await Body.main.api.Helix.Users.GetUsersAsync(logins: [targetUsername]);
            TwitchLib.Api.Helix.Models.Users.GetUsers.User u = a.Users.Single();

            string generalInfo = GetGeneralInfo(u);

            if (usedArgs.Count == 0) { return generalInfo; }

            string color = await GetColor(u.Id);
            string followers = (await GetFollowers(u.Id)).ToString() + " followers";
            string emotes = await GetEmotes(u.Id);
            string liveInfo = await GetLiveStatus(u.Id, message.Privileges);

            if (usedArgs.Contains("a")) { return new($"{generalInfo} | {color} | {followers} | {emotes} | {liveInfo}"); }

            if (usedArgs.Contains("g")) { response += generalInfo + " "; }

            if (usedArgs.Contains("c")) { response += color + " "; }

            if (usedArgs.Contains("f")) { response += followers + " "; }

            if (usedArgs.Contains("e")) { response += emotes + " "; }

            if (usedArgs.Contains("s")) { response += liveInfo + " "; }

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

    private static async Task<string> GetEmotes(string userID)
    {
        try
        {
            var emotes = await Body.main.api.Helix.Chat.GetChannelEmotesAsync(userID);
            string result = "no emotes";

            if (emotes.ChannelEmotes.Length > 0)
            {
                result = emotes.ChannelEmotes.Length + " emotes";
                result += " (" + emotes.ChannelEmotes.Count(e => e.Tier == "1000") + "-T1; " +
                    emotes.ChannelEmotes.Count(e => e.Tier == "2000") + "-T2; " +
                    emotes.ChannelEmotes.Count(e => e.Tier == "3000") + "-T3; " +
                    emotes.ChannelEmotes.Count(e => e.EmoteType == "follower") + "-Flw; " +
                    emotes.ChannelEmotes.Count(e => e.EmoteType == "bitstier") + "-Bits)";
            }
            return result;
        }
        catch
        {
            return Utils.Responses.Surprise.ToString();
        }
    }

    private static async Task<string> GetColor(string userID)
    {
        try
        {
            var color = await Body.main.api.Helix.Chat.GetUserChatColorAsync([userID]);
            return color.Data.Single().Color.Equals("") ? "color not set" : color.Data.Single().Color;
        }
        catch
        {
            return Utils.Responses.Surprise.ToString();
        }
    }

    private static async Task<string> GetLiveStatus(string userID, Privileges p)
    {
        try
        {
            string result = "";

            var liveStatus = await Body.main.api.Helix.Streams.GetStreamsAsync(userIds: [userID]);

            TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream? s = liveStatus.Streams.SingleOrDefault();
            if (s != null)
            {
                result = (p >= Privileges.Trusted) ? $"Now {s.Type} : {s.GameName} - \" {s.Title} \" for {s.ViewerCount} viewers." : $"live {s.GameName}";
            }
            else
            {
                result = "offline";
            }
            return result;
        }
        catch
        {
            return Utils.Responses.Surprise.ToString();
        }
    }

    private static string GetGeneralInfo(TwitchLib.Api.Helix.Models.Users.GetUsers.User user)
    {
        try
        {
            if(!Utils.TryGetUser("lorem ipsum dolor sit amet ", out User dbData, user.Id))
            {
                return Utils.Responses.Surprise.ToString();
            }

            List<string>? previousUsernames = dbData?.PreviousUsernames;
            string offlinerScoreText = "";

            float offlinerScore = Userscore.GetRatioAndScore(dbData!).score;
            offlinerScoreText = $"offliner score: {offlinerScore}.";

            string aka = "";
            if (previousUsernames is not null && previousUsernames.Count != 0)
            {
                aka = "aka: ";
                foreach (string name in previousUsernames)
                {
                    aka += name + ", ";
                }
                aka = aka[..^2] + ". ";
            }

            return $"{user.Type} {user.BroadcasterType} {user.Login} {aka} (id:{user.Id}) {offlinerScoreText} created: {user.CreatedAt:dd/MM/yyyy}";
        }
        catch
        {
            return Utils.Responses.Surprise.ToString();
        }
    }

    private static async Task<int> GetFollowers(string userID) => (await Body.main.api.Helix.Channels.GetChannelFollowersAsync(userID)).Total;
}
