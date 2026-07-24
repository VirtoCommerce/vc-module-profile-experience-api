using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RemoveMemberFromOrganizationCommandHandler : IRequestHandler<RemoveMemberFromOrganizationCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IOrganizationMembershipService _organizationMembershipService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;

        public RemoveMemberFromOrganizationCommandHandler(
            IContactAggregateRepository contactAggregateRepository,
            IOrganizationMembershipService organizationMembershipService,
            IOrganizationMembershipSearchService organizationMembershipSearchService)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _organizationMembershipService = organizationMembershipService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
        }

        public async Task<ContactAggregate> Handle(RemoveMemberFromOrganizationCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.ContactId);
            contactAggregate.Contact.Organizations?.Remove(request.OrganizationId);
            await _contactAggregateRepository.SaveAsync(contactAggregate);

            await RemoveMembershipAsync(contactAggregate, request.OrganizationId);

            return contactAggregate;
        }

        private async Task RemoveMembershipAsync(ContactAggregate contactAggregate, string organizationId)
        {
            var userId = contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var membership = await _organizationMembershipSearchService.GetMembershipAsync(userId, organizationId);

            if (membership != null)
            {
                await _organizationMembershipService.DeleteAsync([membership.Id]);
            }
        }
    }
}
