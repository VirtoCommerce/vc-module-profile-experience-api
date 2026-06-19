using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class LockOrganizationContactCommandHandler : IRequestHandler<LockOrganizationContactCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IOrganizationMembershipService _organizationMembershipService;

        public LockOrganizationContactCommandHandler(
            IContactAggregateRepository contactAggregateRepository,
            IOrganizationMembershipService organizationMembershipService)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _organizationMembershipService = organizationMembershipService;
        }

        public virtual async Task<ContactAggregate> Handle(LockOrganizationContactCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                throw new InvalidOperationException("OrganizationId is required for organization-scoped lock.");
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' not found.");

            var userId = contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return contactAggregate;
            }

            var membership = await _organizationMembershipService.GetByUserAndOrgAsync(userId, request.OrganizationId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' has no membership in organization '{request.OrganizationId}'.");

            await _organizationMembershipService.LockAsync(membership.Id);

            return contactAggregate;
        }
    }
}
