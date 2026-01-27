namespace StreamSchedule.Markov2.Data;

public class TokenPairOnline : TokenPair
{
    public static TokenPairOnline FromTokenPair(TokenPair tp)
    {
        return new TokenPairOnline() { ID = tp.ID, TokenID = tp.TokenID, NextTokenID = tp.NextTokenID, Count = tp.Count};
    }
}
