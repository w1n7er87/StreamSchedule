using Newtonsoft.Json;
using Quartz;
using StreamSchedule.Extensions;
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
            List<string> removed = [.. Emotes.Except(emotes)];
            List<string> added = [.. emotes.Except(Emotes)];

            if (removed.Count != 0 && added.Count != 0)
            {
                response.Append(DiffChanges(added.Select(x => JsonConvert.DeserializeObject<ChannelEmote>(x)).ToList()!, removed.Select(x => JsonConvert.DeserializeObject<ChannelEmote>(x)).ToList()!));
                response.Append(PingList);
                BotCore.SendLongMessage(OutputChannelName, null, response.ToString());
                BotCore.Nlog.Info(response);
                context.JobDetail.JobDataMap.Put("Emotes", emotes);
                return;
            }

            if (removed.Count != 0)
            {
                hadChanges = true;
                response.Append($"{removed.Count} emotes removed 📤 : ").Append(string.Join(" ", DeserializeEmotes(removed)));
            }

            if (added.Count != 0)
            {
                hadChanges = true;
                response.Append($"{added.Count} emotes added 📥 : ").Append(string.Join(" ", DeserializeEmotes(added)));
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

    private static string DiffChanges(List<ChannelEmote> added, List<ChannelEmote> removed)
    {
        List<string> changed = [];
        List<string> addedNew = [];
        List<string> removedForever = [];

        foreach (ChannelEmote addedEmote in added)
        {
            bool didChange = false;
            foreach (ChannelEmote removedEmote in removed)
            {
                if (addedEmote.DeserializeChangedEmote(removedEmote, out string result))
                {
                    didChange = true;
                    removed.Remove(removedEmote);
                    changed.Add(result);
                    continue;
                }
            }
            if (didChange) continue;
            addedNew.Add(addedEmote.EmoteToString());
        }
        removedForever = [.. removed.Select(e => e.EmoteToString())];
        return $"{removedForever.Count} emotes removed 📤 : {string.Join(" ", removedForever)} , {addedNew.Count} added 📥 : {string.Join(" ", addedNew)}" + (changed.Count != 0 ? $" { changed.Count} changed ♻️ : {string.Join(" ", changed)}" : "");
    }

    private static IEnumerable<string> DeserializeEmotes(IEnumerable<string> serializedEmotes)
    {
        return serializedEmotes.Select(x => JsonConvert.DeserializeObject<ChannelEmote>(x).EmoteToString());
    }
}
