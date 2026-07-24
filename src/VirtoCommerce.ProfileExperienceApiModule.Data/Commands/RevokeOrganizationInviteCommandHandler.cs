using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RevokeOrganizationInviteCommandHandler : IRequestHandler<RevokeOrganizationInviteCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;
        private readonly IInviteCustomerService _inviteCustomerService;

        public RevokeOrganizationInviteCommandHandler(
            IContactAggregateRepository contactAggregateRepository,
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            IInviteCustomerService inviteCustomerService)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _organizationMembershipSearchService = organizationMembershipSearchService;
            _inviteCustomerService = inviteCustomerService;
        }

        public virtual async Task<ContactAggregate> Handle(RevokeOrganizationInviteCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                throw new InvalidOperationException("OrganizationId is required for revoking an organization invite.");
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' not found.");

            var userId = contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return contactAggregate;
            }

            var membership = await OrganizationInviteHelper.GetPendingInviteAsync(
                _organizationMembershipSearchService, userId, request.OrganizationId);

            await _inviteCustomerService.RevokeInviteAsync(membership.Id, cancellationToken);

            return await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId);
        }
    }
}
