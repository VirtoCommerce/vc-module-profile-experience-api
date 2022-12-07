using System;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetOrganizationByIdQuery : GetMemberByIdQueryBase<OrganizationAggregate>
{
    public GetOrganizationByIdQuery()
    {
    }

    [Obsolete("Use parameterless constructor with object initialization")]
    public GetOrganizationByIdQuery(string organizationId)
    {
        OrganizationId = organizationId;
    }

    [Obsolete("Use Id instead")]
    public string OrganizationId
    {
        get => Id;
        set => Id = value;
    }
}
