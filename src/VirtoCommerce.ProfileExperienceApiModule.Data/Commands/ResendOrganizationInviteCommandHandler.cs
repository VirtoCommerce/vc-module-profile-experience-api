using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ResendOrganizationInviteCommandHandler : IRequestHandler<ResendOrganizationInviteCommand, IdentityResultResponse>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;
        private readonly IInviteCustomerService _inviteCustomerService;

        public ResendOrganizationInviteCommandHandler(
            IContactAggregateRepository contactAggregateRepository,
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            IInviteCustomerService inviteCustomerService)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _organizationMembershipSearchService = organizationMembershipSearchService;
            _inviteCustomerService = inviteCustomerService;
        }

        public virtual async Task<IdentityResultResponse> Handle(ResendOrganizationInviteCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                throw new InvalidOperationException("OrganizationId is required for resending an organization invite.");
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId);
            var userId = contactAggregate?.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return new IdentityResultResponse
                {
                    Succeeded = false,
                    Errors = [new IdentityErrorInfo { Code = "UserNotFound", Description = "Invited user not found" }],
                };
            }

            var membership = await OrganizationInviteHelper.GetPendingInviteAsync(
                _organizationMembershipSearchService, userId, request.OrganizationId);

            var resendResult = await _inviteCustomerService.ResendInviteAsync(
                membership.Id, request.UrlSuffix, request.Message, cancellationToken);

            return OrganizationInviteHelper.ToIdentityResultResponse(resendResult);
        }
    }
}
