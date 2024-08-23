using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamSchedule.Data.Models;

[PrimaryKey("StreamTime")]
public class Stream
{
    public DateTime StreamTime { get; set; }
    public string? StreamTitle { get; set; }
}
