using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamSchedule.Data.Models;

[PrimaryKey("StreamDate")]
public class Stream
{
    public DateOnly StreamDate { get; set; }
    public TimeOnly StreamTime { get; set; }
    public string? StreamTitle { get; set; }
}
