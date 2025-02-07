namespace StreamSchedule.GraphQL.Data;

public class UserRoles
{
    public bool IsAffiliate { get; set; }
    public bool IsPartner { get; set; }
    public bool? IsSiteAdmin { get; set; }
    public bool? IsGlobalMod { get; set; }
    public bool? IsStaff { get; set; }
}
