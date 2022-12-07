using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetOrganizationByIdQueryHandler : GetMemberByIdQueryHandlerBase<GetOrganizationByIdQuery, OrganizationAggregate>
{
    public GetOrganizationByIdQueryHandler(IOrganizationAggregateRepository contactAggregateRepository) : base(contactAggregateRepository)
    {
    }
}
