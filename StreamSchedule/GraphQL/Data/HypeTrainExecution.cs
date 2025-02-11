namespace StreamSchedule.GraphQL.Data;

public class HypeTrainExecution
{
    public HypeTrainConfig? Config { get; set; }
    public HypeTrainProgress? Progress { get; set; }
    public bool? IsActive { get; set; }
}
