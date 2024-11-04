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
        List<string> removed = Emotes.Except(emotes).ToList();
        List<string> added = emotes.Except(Emotes).ToList();

        if (removed.Count != 0)
        {
            hadChanges = true;
            response += "emotes removed: " + string.Join(" ", removed);
        }

        if (added.Count != 0)
        {
            hadChanges = true;
            response += "emotes added: " + string.Join(" ", added);
        }

        context.JobDetail.JobDataMap.Put("Emotes", emotes);
        Console.WriteLine($"{(hadChanges? response : "no")} changes to global emotes ");
        if (hadChanges) 
        {
            BotCore.Client.SendMessage("vedal987", response);
            BotCore.GlobalEmotes = ee;
        }
    }
}
