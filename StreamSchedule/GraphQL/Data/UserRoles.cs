namespace StreamSchedule.GraphQL.Data;

public record UserRoles(
    bool? IsAffiliate,
    bool? IsPartner,
    bool? IsSiteAdmin,
    bool? IsGlobalMod,
    bool? IsStaff
);
