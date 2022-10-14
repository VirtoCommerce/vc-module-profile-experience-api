using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ChangeOrganizationContactRoleCommand : ICommand<IdentityResultResponse>
    {
        public string UserId { get; set; }
        public string[] RoleIds { get; set; }
    }
}
