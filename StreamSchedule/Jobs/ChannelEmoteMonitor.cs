using Newtonsoft.Json;
using Quartz;
using System.Text;
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
            StringBuilder response = new(Username);
            ChannelEmote[] ee = (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(UserID)).ChannelEmotes;
            List<string> emotes = [.. ee.Select(x => JsonConvert.SerializeObject(x))];

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
                hadChanges = true;
                response.Append(" emotes removed: ").Append(string.Join(" ", DeserializeEmotes(removed)));
            }

            if (added.Any())
            {
                hadChanges = true;
                response.Append(" emotes added: ").Append(string.Join(" ", DeserializeEmotes(added)));
            }

            context.JobDetail.JobDataMap.Put("Emotes", emotes);
    
            if (hadChanges) 
            {
                response.Append(PingList);
                BotCore.SendLongMessage(OutputChannelName, null, response.ToString());
                BotCore.Nlog.Info(response);
            }
        }
        catch
        {
            BotCore.Nlog.Debug($"Failed to get emotes for {Username}");
        }
    }

    private static IEnumerable<string> DeserializeEmotes(IEnumerable<string> serializedEmotes)
    {
        return serializedEmotes.Select(x =>
        {
            ChannelEmote? e = JsonConvert.DeserializeObject<ChannelEmote>(x);
            return $"{e?.Name} ({e?.Tier switch
            {
                "1000" => "T1",
                "2000" => "T2",
                "3000" => "T3",
                _ => ""
            }}{e?.EmoteType switch
            {
                "bitstier" => "B",
                "follower" => "F",
                _ =>""
            }}{((e?.Format.Contains("animated") ?? false) ? "A" : "")})";
        });
    }
}
