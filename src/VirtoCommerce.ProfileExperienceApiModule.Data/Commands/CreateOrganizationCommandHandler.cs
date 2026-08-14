using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, OrganizationAggregate>
    {
        protected readonly IProfileExperienceApiModuleMapper _mapper;
        protected readonly IOrganizationAggregateRepository _organizationAggregateRepository;
        protected readonly IMemberAggregateFactory _memberAggregateFactory;
        protected readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        protected readonly OrganizationValidator _organizationValidator;

        public CreateOrganizationCommandHandler(IProfileExperienceApiModuleMapper mapper,
            IOrganizationAggregateRepository organizationAggregateRepository,
            IMemberAggregateFactory factory,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            OrganizationValidator organizationValidator)
        {
            _mapper = mapper;
            _organizationAggregateRepository = organizationAggregateRepository;
            _memberAggregateFactory = factory;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _organizationValidator = organizationValidator;
        }

        public virtual async Task<OrganizationAggregate> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var org = _mapper.ToOrganization(request);

            await _organizationValidator.ValidateAndThrowAsync(org, cancellationToken);

            var orgAggr = _memberAggregateFactory.Create<OrganizationAggregate>(org);

            if (request.DynamicProperties != null)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(orgAggr.Organization, request.DynamicProperties);
            }

            await _organizationAggregateRepository.SaveAsync(orgAggr);

            return orgAggr;
        }
    }
}
