using StreamSchedule.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace StreamSchedule.Commands;

internal class AddStream : Command
{
    internal override string Call => "!test";

    internal override bool Handle(ChatMessage message)
    {
        string[] split = message.Message.Split(" ");
        if (split.Length < 4) { return false; }

        DateTime temp = DateTime.Now;

        if (!DateTime.TryParse(split[1] + DateTime.UtcNow.Year + " " + split[2], out temp))
        {
            return false;
        }
        temp = temp.ToUniversalTime();
        Data.Models.Stream stream = new()
        {
            StreamTime = temp, StreamTitle = split[3]
        };

        try
        {
            Data.Models.Stream? s = Body.dbContext.Streams.SingleOrDefault(x => x.StreamTime == temp);
            if (s == null)
            {
                Body.dbContext.Streams.Add(stream);
            }
            else
            {
                Body.dbContext.Streams.Update(stream);
            }
            Body.dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }
}
