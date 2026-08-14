using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, OrganizationAggregate>
    {
        private readonly IProfileExperienceApiModuleMapper _mapper;
        private readonly IOrganizationAggregateRepository _organizationAggregateRepository;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        private readonly OrganizationValidator _organizationValidator;

        public UpdateOrganizationCommandHandler(
            IProfileExperienceApiModuleMapper mapper,
            IOrganizationAggregateRepository organizationAggregateRepository,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            OrganizationValidator organizationValidator)
        {
            _mapper = mapper;
            _organizationAggregateRepository = organizationAggregateRepository;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _organizationValidator = organizationValidator;
        }

        public virtual async Task<OrganizationAggregate> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var organizationAggregate = await _organizationAggregateRepository.GetMemberAggregateRootByIdAsync<OrganizationAggregate>(request.Id);
            _mapper.MapTo(request, organizationAggregate.Organization);

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
