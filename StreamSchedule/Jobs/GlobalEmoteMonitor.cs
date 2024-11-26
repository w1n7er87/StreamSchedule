using Quartz;
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
            string response = "Twitch emotes - ";
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
                response += "emotes removed: " + string.Join(" ", removed);
            }

            if (added.Any())
            {
                hadChanges = true;
                response += "emotes added: " + string.Join(" ", added);
            }

            context.JobDetail.JobDataMap.Put("Emotes", emotes);
            if (hadChanges)
            {
                Console.WriteLine($"{response} changes to global emotes ");
                BotCore.Client.SendMessage("vedal987", response);
                BotCore.Client.SendMessage("w1n7er", response);
                BotCore.GlobalEmotes = ee;
            }
        }
        catch
        {
            Console.WriteLine("failed to get global emotes");
        }
    }
}
