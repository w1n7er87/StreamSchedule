namespace StreamSchedule.GraphQL.Data;

public record HypeTrainParticipation(HypeTrainAction? Action, HypeTrainActionSource? Source, int? Quantity)
{
    public override string ToString()
    {
        string action = Action switch
        {
            HypeTrainAction.TIER_1_SUB => "T1 sub",
            HypeTrainAction.TIER_1_GIFTED_SUB => "T1 gift",
            HypeTrainAction.TIER_2_SUB => "T2 sub",
            HypeTrainAction.TIER_2_GIFTED_SUB => "T2 gift",
            HypeTrainAction.TIER_3_SUB => "T3 sub",
            HypeTrainAction.TIER_3_GIFTED_SUB => "T3 gift",
            HypeTrainAction.CHEER => "Cheer",
            HypeTrainAction.POLLS => "other bits",
            HypeTrainAction.BITS_ON_EXTENSION => "extensions",
            HypeTrainAction.UNKNOWN => "unknown",
            _ or null => "",
        };
        
        string source = Source switch
        {
            HypeTrainActionSource.UNKNOWN => "",
            HypeTrainActionSource.BITS => "bits",
            HypeTrainActionSource.SUBS => "subs",
            _ or null => "",
        };
        return $"{action} - {Quantity}";
    }
}
