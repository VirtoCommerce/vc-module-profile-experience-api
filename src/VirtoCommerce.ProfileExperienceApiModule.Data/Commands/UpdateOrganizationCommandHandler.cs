using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, OrganizationAggregate>
    {
        private readonly IOrganizationAggregateRepository _organizationAggregateRepository;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;

        public UpdateOrganizationCommandHandler(
            IOrganizationAggregateRepository organizationAggregateRepository,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater)
        {
            _organizationAggregateRepository = organizationAggregateRepository;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
        }

        public virtual async Task<OrganizationAggregate> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var organizationAggregate = await _organizationAggregateRepository.GetMemberAggregateRootByIdAsync<OrganizationAggregate>(request.Id);
            _ = request.MapTo(organizationAggregate.Organization);

            if (request.DynamicProperties != null)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(organizationAggregate.Organization, request.DynamicProperties);
            }

            await _organizationAggregateRepository.SaveAsync(organizationAggregate);

            return organizationAggregate;
        }
    }
}
