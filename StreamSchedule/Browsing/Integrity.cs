namespace StreamSchedule.Browsing;

public readonly struct Integrity(string Token, string DeviceID)
{
    public readonly string Token = Token;
    public readonly string DeviceID = DeviceID;
}
