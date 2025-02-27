using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Markov.Data;

[PrimaryKey(nameof(ID))]
public class WordCountPair
{
    public int ID { get; set; }
    public int LinkID { get; set; }
    public LinkStored Link { get; set; }
    public string Word { get; set; }
    public int Count { get; set; }
}
