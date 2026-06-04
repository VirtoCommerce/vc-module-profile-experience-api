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
                throw new ArgumentException("OrganizationId is required for organization-scoped lock.", nameof(request.OrganizationId));
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.UserId)
                ?? throw new ArgumentException($"Contact '{request.UserId}' not found.", nameof(request.UserId));

            var userId = contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return contactAggregate;
            }

            var membership = await _organizationMembershipService.GetByUserAndOrgAsync(userId, request.OrganizationId);
            if (membership != null)
            {
                await _organizationMembershipService.LockAsync(membership.Id);
            }

            return contactAggregate;
        }
    }
}
