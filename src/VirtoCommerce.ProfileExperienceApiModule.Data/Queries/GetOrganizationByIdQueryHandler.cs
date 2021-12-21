using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetOrganizationByIdQueryHandler : IQueryHandler<GetOrganizationByIdQuery, OrganizationAggregate>
    {
        private readonly IOrganizationAggregateRepository _organizationAggregateRepository;

        public GetOrganizationByIdQueryHandler(IOrganizationAggregateRepository organizationAggregateRepository)
        {
            _organizationAggregateRepository = organizationAggregateRepository;
        }

        public virtual Task<OrganizationAggregate> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
        {
            return _organizationAggregateRepository.GetMemberAggregateRootByIdAsync<OrganizationAggregate>(request.OrganizationId);
        }
    }
}
