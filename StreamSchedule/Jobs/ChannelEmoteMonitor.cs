using Newtonsoft.Json;
using Quartz;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace StreamSchedule.Jobs;

[PersistJobDataAfterExecution, DisallowConcurrentExecution]
internal class ChannelEmoteMonitor : IJob
{
    public string UserID { private get; set; }
    public string Username { private get; set; }
    public bool FirstRun { private get; set; }
    public string OutputChannelName { private get; set; }
    public string PingList { private get; set; }

    public List<string> Emotes { private get; set; }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            string response = Username;
            ChannelEmote[] ee = (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(UserID)).ChannelEmotes;
            List<string> emotes = [];
            foreach (var emote in ee)
            {
                emotes.Add(JsonConvert.SerializeObject(emote));
            }

            if (FirstRun)
            {
                context.JobDetail.JobDataMap.Put("Emotes", emotes);
                context.JobDetail.JobDataMap.Put("FirstRun", false);
                return;
            }

            bool hadChanges = false;
            IEnumerable<string> removed = Emotes.Except(emotes);
            IEnumerable<string> added = emotes.Except(Emotes);

            if (removed.Any())
            {
                removed = removed.Select(x => 
                {
                    ChannelEmote? e = JsonConvert.DeserializeObject<ChannelEmote>(x);
                    return $"{e?.Name} ({e?.Tier switch {
                        "1000" => "T1",
                        "2000" => "T2",
                        "3000" => "T3",
                        "bitstier" => "B",
                        "follower" => "F",
                        _ => ""
                    }}{((e?.Format.Contains("animated") ?? false)? "A" : "")})";
                });

                hadChanges = true;
                response += " emotes removed: " + string.Join(" ", removed);
            }

            if (added.Any())
            {
                added = added.Select(x =>
                {
                    ChannelEmote? e = JsonConvert.DeserializeObject<ChannelEmote>(x);
                    return $"{e?.Name} ({e?.Tier switch
                    {
                        "1000" => "T1",
                        "2000" => "T2",
                        "3000" => "T3",
                        "bitstier" => "B",
                        "follower" => "F",
                        _ => ""
                    }}{((e?.Format.Contains("animated") ?? false) ? "A" : "")})";
                });

                hadChanges = true;
                response += " emotes added: " + string.Join(" ", added);
            }

            context.JobDetail.JobDataMap.Put("Emotes", emotes);
    
            if (hadChanges) 
            {
                response += PingList;
                BotCore.Client.SendMessage(OutputChannelName, response);
                BotCore.Nlog.Info($"{response} changes to {Username} emotes ");
            }
        }
        catch
        {
            BotCore.Nlog.Debug($"Failed to get emotes for {Username}");
        }
    }
}
