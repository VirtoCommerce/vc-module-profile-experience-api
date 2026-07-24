using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    internal static class OrganizationInviteHelper
    {
        public static async Task<OrganizationMembership> GetPendingInviteAsync(
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            string userId,
            string organizationId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(organizationId))
            {
                throw new InvalidOperationException("UserId and OrganizationId are required.");
            }

            var membership = await organizationMembershipSearchService.GetMembershipAsync(userId, organizationId);
            if (membership == null || membership.Status != VirtoCommerce.CustomerModule.Core.ModuleConstants.MembershipStatuses.Invited)
            {
                throw new InvalidOperationException($"No pending invite found for organization '{organizationId}'.");
            }

            return membership;
        }

        public static async Task<ContactAggregate> GetContactAggregateAsync(
            IContactAggregateRepository contactAggregateRepository,
            Func<UserManager<ApplicationUser>> userManagerFactory,
            string userId)
        {
            using var userManager = userManagerFactory();

            var user = await userManager.FindByIdAsync(userId)
                ?? throw new InvalidOperationException($"User '{userId}' not found.");

            return await contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(user.MemberId);
        }

        public static IdentityResultResponse ToIdentityResultResponse(InviteCustomerResult inviteResult)
        {
            return new IdentityResultResponse
            {
                Succeeded = inviteResult.Succeeded,
                Errors = (inviteResult.Errors ?? new List<InviteCustomerError>())
                    .Select(error => new IdentityErrorInfo
                    {
                        Code = error.Code,
                        Description = error.Description,
                        Parameter = error.Parameter,
                    })
                    .ToList(),
            };
        }
    }
}
