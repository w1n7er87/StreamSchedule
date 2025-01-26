using Quartz;
using System.Text;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace StreamSchedule.Jobs;

[PersistJobDataAfterExecution, DisallowConcurrentExecution]
internal class GlobalEmoteMonitor : IJob
{
    public bool FirstRun { private get; set; }
    public List<string> Emotes { private get; set; }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            StringBuilder response = new("Twitch emotes -");
            GlobalEmote[] ee = (await BotCore.API.Helix.Chat.GetGlobalEmotesAsync()).GlobalEmotes;
            List<string> emotes = [];

            foreach (var emote in ee)
            {
                emotes.Add(emote.Name);
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
                hadChanges = true;
                response.Append(" emotes removed: ").Append(string.Join(" ", removed));
            }

            if (added.Any())
            {
                hadChanges = true;
                response.Append(" emotes added: ").Append(string.Join(" ", added));
            }

            context.JobDetail.JobDataMap.Put("Emotes", emotes);

            if (hadChanges)
            {
                string r = response.ToString();
                BotCore.SendLongMessage("vedal987", null, r);
                BotCore.SendLongMessage("w1n7er", null, r);
                BotCore.Nlog.Info(r);
                BotCore.GlobalEmotes = ee;
            }
        }
        catch
        {
            BotCore.Nlog.Debug("failed to get global emotes");
        }
    }
}
