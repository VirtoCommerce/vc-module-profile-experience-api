using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RejectOrganizationInviteCommandHandler : IRequestHandler<RejectOrganizationInviteCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IOrganizationMembershipService _organizationMembershipService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

        public RejectOrganizationInviteCommandHandler(
            IContactAggregateRepository contactAggregateRepository,
            IOrganizationMembershipService organizationMembershipService,
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            Func<UserManager<ApplicationUser>> userManagerFactory)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _organizationMembershipService = organizationMembershipService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
            _userManagerFactory = userManagerFactory;
        }

        public virtual async Task<ContactAggregate> Handle(RejectOrganizationInviteCommand request, CancellationToken cancellationToken)
        {
            var membership = await OrganizationInviteHelper.GetPendingInviteAsync(
                _organizationMembershipSearchService, request.UserId, request.OrganizationId);

            await _organizationMembershipService.SetStatusAsync(
                membership.Id, CustomerModuleConstants.MembershipStatuses.Rejected);

            var contactAggregate = await OrganizationInviteHelper.GetContactAggregateAsync(_contactAggregateRepository, _userManagerFactory, request.UserId);
            contactAggregate.Contact.Organizations?.Remove(request.OrganizationId);

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }
    }
}
