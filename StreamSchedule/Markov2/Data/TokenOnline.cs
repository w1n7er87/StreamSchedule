namespace StreamSchedule.Markov2.Data;

public class TokenOnline : Token
{
    public static TokenOnline FromToken(Token t)
    {
        return new TokenOnline() { ID = t.ID, TokenID = t.TokenID, Value = t.Value };
    }
}
