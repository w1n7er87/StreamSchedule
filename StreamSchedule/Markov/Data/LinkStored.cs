using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Markov.Data;

[PrimaryKey(nameof(ID))]
public class LinkStored
{
    public int ID { get; set; }
    public string Key { get; set; }
    public ICollection<WordCountPair> NextWords { get; set; } = new List<WordCountPair>();
}
