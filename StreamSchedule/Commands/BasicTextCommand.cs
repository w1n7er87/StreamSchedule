using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class BasicTextCommand : Command
{
    internal override string Call => "cmd";
    internal override Privileges MinPrivilege => Privileges.Mod;
    internal override string Help => "manage simple text commands: -add/-rm (-p[priv] optional) [command name](required) [command content](required) ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => ["add", "rm", "p"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        List<TextCommand> commands = [.. Body.dbContext.TextCommands];
        string text = Utils.RetrieveArguments(Arguments!, message.Message, out Dictionary<string, string> usedArguments);
        string commandName = text.Split(' ')[0];
        text = text[commandName.Length ..];
        Console.WriteLine(text);
        if (string.IsNullOrEmpty(commandName)) return Task.FromResult(Utils.Responses.Fail + (" no command name provided "));

        if(commandName.Length < 2) return Task.FromResult(Utils.Responses.Fail + (" command name should be 2 characters or longer "));

        if (usedArguments.TryGetValue("add", out string? _))
        {
            if (string.IsNullOrEmpty(text)) return Task.FromResult(Utils.Responses.Fail + (" no content provided "));

            if (commands.Count > 0)
            {
                if (commands.FirstOrDefault(x => x.Name.Equals(commandName)) is not null || Body.CurrentCommands.Any(x => x?.Call == commandName))
                {
                    return Task.FromResult(Utils.Responses.Fail + " command with this name already exists. ");
                }
                else
                {
                    Privileges p = usedArguments.TryGetValue("p", out string? pp) ? Utils.ParsePrivilege(pp) : Privileges.None;
                    Body.dbContext.TextCommands.Add(new() { Name = commandName, Content = text, Privileges = p });
                    Body.dbContext.SaveChanges();
                    return Task.FromResult(Utils.Responses.Ok);
                }
            }
            else
            {
                Privileges p = usedArguments.TryGetValue("p", out string? pp) ? Utils.ParsePrivilege(pp) : Privileges.None;
                Body.dbContext.TextCommands.Add(new() { Name = commandName, Content = text, Privileges = p });
                Body.dbContext.SaveChanges();
                return Task.FromResult(Utils.Responses.Ok);
            }
        }
        else if (usedArguments.TryGetValue("rm", out string? _))
        {
            if (commands.Count <= 0)
            {
                return Task.FromResult(Utils.Responses.Fail + " there are no custom commands. ");
            }
            TextCommand? c = Body.dbContext.TextCommands.FirstOrDefault(x => x.Name == commandName);
            if (c is null)
            {
                return Task.FromResult(Utils.Responses.Fail + " there is no command with this name. ");
            }
            Body.dbContext.TextCommands.Remove(c);
            Body.dbContext.SaveChanges();
            return Task.FromResult(Utils.Responses.Ok);
        }
        return Task.FromResult(Utils.Responses.Surprise);
    }
}
