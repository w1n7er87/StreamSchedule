using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Markov2.Data;

[PrimaryKey(nameof(ID))]
public class Token()
{
    public Token(int tokenId, string value) :  this()
    {
        TokenID = tokenId;
        Value = value;
    }
    
    public int ID;
    public int TokenID {get; set;}
    public string Value {get; set;}
}
