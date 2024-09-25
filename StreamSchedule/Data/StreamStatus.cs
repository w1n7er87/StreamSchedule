namespace StreamSchedule.Data;

public enum StreamStatus
{
    Cancelled = -2,
    Moved = -1,
    Confirmed = 0,
    TBD = 1
}

public static class StreamStatusUtils
{
    public static StreamStatus ParseFromString(string str)
    {
        return str.ToLower() switch
        {
            "canceled" => StreamStatus.Cancelled,
            "moved" => StreamStatus.Cancelled,
            "confirmed" => StreamStatus.Confirmed,
            "tbd" => StreamStatus.TBD,
            _ => StreamStatus.Confirmed
        };
    }
}