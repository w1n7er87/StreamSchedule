using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Markov2.Data;

[PrimaryKey(nameof(ID))]
public class TokenPair()
{
    public TokenPair(int tokenID, int nextID, int count) : this()
    {
        TokenID = tokenID;
        NextTokenID = nextID;
        Count = count;
    }
    
    public int ID;
    public int TokenID { get; set; } 
    public int NextTokenID {get; set;}
    public int Count { get; set; } 
}
