using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, OrganizationAggregate>
    {
        private readonly IOrganizationAggregateRepository _organizationAggregateRepository;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        private readonly OrganizationValidator _organizationValidator;

        public UpdateOrganizationCommandHandler(
            IOrganizationAggregateRepository organizationAggregateRepository,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            OrganizationValidator organizationValidator)
        {
            _organizationAggregateRepository = organizationAggregateRepository;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _organizationValidator = organizationValidator;
        }

        public virtual async Task<OrganizationAggregate> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var organizationAggregate = await _organizationAggregateRepository.GetMemberAggregateRootByIdAsync<OrganizationAggregate>(request.Id);
            _ = request.MapTo(organizationAggregate.Organization);

            await _organizationValidator.ValidateAndThrowAsync(organizationAggregate.Organization, cancellationToken);

            if (request.DynamicProperties != null)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(organizationAggregate.Organization, request.DynamicProperties);
            }

            await _organizationAggregateRepository.SaveAsync(organizationAggregate);

            return organizationAggregate;
        }
    }
}
