using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class UserInfo : Command
{
    internal override string Call => "whois";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "user info: [username]";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override async Task<string> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        string response = "";

        int userID = 0;
        bool idProvided = false;
        string targetUsername = message.Username;

        if (!split[0].Equals("")) // do i have anything provided
        {
            if (split[0].StartsWith('#'))
            {
                idProvided = int.TryParse(split[0].Replace("#", "").Replace("@", ""), out userID); // if i have numbers with prefix - treat them as userid
            }
             
            if(!idProvided) { targetUsername = split[0].Replace("#", "").Replace("@", ""); } // if there was no prefix or conversion failed, treat it as a username
        }

        int c = 5;
        while (true)
        {
            try
            {
                var a = idProvided ? await Body.main.api.Helix.Users.GetUsersAsync(ids: [userID.ToString()]) : await Body.main.api.Helix.Users.GetUsersAsync(logins: [targetUsername]);
                TwitchLib.Api.Helix.Models.Users.GetUsers.User u = a.Users.Single();
                userID = int.Parse(u.Id);

                var emotes = await Body.main.api.Helix.Chat.GetChannelEmotesAsync(userID.ToString());
                var color = await Body.main.api.Helix.Chat.GetUserChatColorAsync([userID.ToString()]);
                var liveStatus = await Body.main.api.Helix.Streams.GetStreamsAsync(userIds: [userID.ToString()]);

                string isLive = "";
                TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream? s = liveStatus.Streams.SingleOrDefault();
                if ( s != null) 
                {
                    isLive += $" Now {s.Type} : { s.Title } with { s.ViewerCount } viewers. ";
                }
                else
                {
                    isLive = "offline";
                }

                string nameOrID = idProvided ? "name: " + u.Login : "id: " + userID;
                string emotesPerTier = "";

                if (emotes.ChannelEmotes.Length > 0)
                {
                    emotesPerTier = emotes.ChannelEmotes.Count() + " emotes";
                    emotesPerTier += " ("+ emotes.ChannelEmotes.Count(e => e.Tier == "1000") + "-T1; " +
                        emotes.ChannelEmotes.Count(e => e.Tier == "2000") + "-T2; " +
                        emotes.ChannelEmotes.Count(e => e.Tier == "3000") + "-T3; " +
                        emotes.ChannelEmotes.Count(e => e.EmoteType == "follower") + "-Flw; " +
                        emotes.ChannelEmotes.Count(e => e.EmoteType == "bitstier") + "-Bits. ";
                }

                Data.Models.User? dbData = Body.dbContext.Users.SingleOrDefault(x => x.Id == userID);
                string aka = "";

                List<string>? previousUsernames = dbData?.PreviousUsernames;

                if (dbData != null && previousUsernames != null && previousUsernames.Count != 0)
                {
                    aka = "aka: ";
                    foreach (string name in previousUsernames)
                    {
                        aka += name +", ";
                    }
                    aka = aka[..^2] + ". ";
                }

                response = nameOrID + " " + aka + u.Type + " created: " + u.CreatedAt.ToString("dd/MM/yyyy")
                    + ". " + u.BroadcasterType + " " + emotesPerTier + " color: " + (color.Data.Single().Color.Equals("") ? "not set" : color.Data.Single().Color) + 
                    " " + isLive;

                break;
            }
            catch(Exception ex)
            {
                if(ex is InvalidOperationException)
                {
                    return Utils.Responses.Fail + " no user with such name/id";
                }

                if (c <= 0) 
                {
                    Console.WriteLine(ex.ToString());
                    return Utils.Responses.Surprise;
                }
                c--;
                await Task.Delay(100);
            }
        }
        return response;
    }
}
