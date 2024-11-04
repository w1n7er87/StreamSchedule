using Quartz;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace StreamSchedule.Jobs;
[PersistJobDataAfterExecution, DisallowConcurrentExecution]
internal class ChannelEmoteMonitor : IJob
{
    public string UserID { private get; set; }
    public string Username { private get; set; }
    public bool FirstRun { private get; set; }
    public List<string> Emotes { private get; set; }
    public async Task Execute(IJobExecutionContext context)
    {
        string response = Username;
        ChannelEmote[] ee = (await BotCore.API.Helix.Chat.GetChannelEmotesAsync(UserID)).ChannelEmotes;
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

        if(removed.Count != 0)
        {
            hadChanges = true;
            response += " emotes removed: " + string.Join(" ", removed);
        }

        if (added.Count != 0)
        {
            hadChanges = true;
            response += " emotes added: " + string.Join(" ", added);
        }

        context.JobDetail.JobDataMap.Put("Emotes", emotes);
        Console.WriteLine($"{(hadChanges ? response : "no")} changes to {Username} emotes ");
        if (hadChanges) BotCore.Client.SendMessage(Username, response);
    }
}
