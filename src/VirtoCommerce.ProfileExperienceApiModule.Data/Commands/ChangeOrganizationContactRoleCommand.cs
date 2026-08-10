using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ChangeOrganizationContactRoleCommand : ICommand<IdentityResultResponse>
    {
        public string MemberId { get; set; }
        public string OrganizationId { get; set; }
        public string StoreId { get; set; }
        public string CultureName { get; set; }
        public string[] RoleIds { get; set; }
    }
}
