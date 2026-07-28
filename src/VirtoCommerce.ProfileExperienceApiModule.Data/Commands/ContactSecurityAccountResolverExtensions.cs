using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    internal static class ContactSecurityAccountResolverExtensions
    {
        public static async Task<(string UserId, OrganizationMembership Membership)> ResolveMembershipForOrganizationAsync(
            this IOrganizationMembershipSearchService organizationMembershipSearchService,
            Contact contact,
            string organizationId)
        {
            var securityAccountIds = contact?.SecurityAccounts?
                .Select(sa => sa.Id)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList() ?? [];

            if (securityAccountIds.Count <= 1)
            {
                return (securityAccountIds.FirstOrDefault(), null);
            }

            var memberships = await organizationMembershipSearchService.SearchAllNoCloneAsync(new OrganizationMembershipSearchCriteria
            {
                UserIds = securityAccountIds,
                OrganizationId = organizationId,
            });

            var membership = memberships.FirstOrDefault();

            return (membership?.UserId ?? securityAccountIds[0], membership);
        }
    }
}
