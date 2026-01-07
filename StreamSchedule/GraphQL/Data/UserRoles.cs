namespace StreamSchedule.GraphQL.Data;

public record UserRoles(
    bool? IsAffiliate,
    bool? IsPartner,
    bool? IsSiteAdmin,
    bool? IsGlobalMod,
    bool? IsStaff,
    bool? IsMonetized,
    bool? IsExtensionsDeveloper,
    bool? IsParticipatingDJ);
