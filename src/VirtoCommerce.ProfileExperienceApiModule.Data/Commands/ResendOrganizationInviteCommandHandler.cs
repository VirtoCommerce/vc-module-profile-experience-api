using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;

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
                return new IdentityResultResponse
                {
                    Succeeded = false,
                    Errors = [new IdentityErrorInfo { Code = "OrganizationIdRequired", Description = "OrganizationId is required for resending an organization invite." }],
                };
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId);
            var (userId, knownMembership) = await _organizationMembershipSearchService.ResolveMembershipForOrganizationAsync(
                contactAggregate?.Contact, request.OrganizationId);

            if (string.IsNullOrEmpty(userId))
            {
                return new IdentityResultResponse
                {
                    Succeeded = false,
                    Errors = [new IdentityErrorInfo { Code = "UserNotFound", Description = "Invited user not found" }],
                };
            }

            var membership = knownMembership ?? await _organizationMembershipSearchService.GetMembershipAsync(userId, request.OrganizationId);
            if (membership == null || membership.Status != CustomerModuleConstants.MembershipStatuses.Invited)
            {
                return new IdentityResultResponse
                {
                    Succeeded = false,
                    Errors = [new IdentityErrorInfo { Code = "InviteNotFound", Description = $"No pending invite found for organization '{request.OrganizationId}'." }],
                };
            }

            var resendResult = await _inviteCustomerService.ResendInviteAsync(
                new ResendInviteRequest { MembershipId = membership.Id, UrlSuffix = request.UrlSuffix, Message = request.Message },
                cancellationToken);

            return OrganizationInviteHelper.ToIdentityResultResponse(resendResult);
        }
    }
}
