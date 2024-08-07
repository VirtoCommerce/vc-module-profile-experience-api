using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetRoleQuery : IQuery<Role>
    {
        public string RoleName { get; set; }

        public GetRoleQuery(string roleName)
        {
            RoleName = roleName;
        }
        public string UserId { get; set; }
    }
}
