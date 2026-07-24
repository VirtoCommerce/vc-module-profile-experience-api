using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UnlockOrganizationContactCommandHandler : IRequestHandler<UnlockOrganizationContactCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IOrganizationMembershipService _organizationMembershipService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;

        public UnlockOrganizationContactCommandHandler(
            IContactAggregateRepository contactAggregateRepository,
            IOrganizationMembershipService organizationMembershipService,
            IOrganizationMembershipSearchService organizationMembershipSearchService)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _organizationMembershipService = organizationMembershipService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
        }

        public virtual async Task<ContactAggregate> Handle(UnlockOrganizationContactCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                throw new InvalidOperationException("OrganizationId is required for organization-scoped unlock.");
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' not found.");

            var userId = contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return contactAggregate;
            }

            var membership = await _organizationMembershipSearchService.GetMembershipAsync(userId, request.OrganizationId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' has no membership in organization '{request.OrganizationId}'.");

            await _organizationMembershipService.UnlockAsync(membership.Id);

            return contactAggregate;
        }
    }
}
