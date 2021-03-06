using System.Linq;
using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class RoleType : ObjectGraphType<Role>
    {
        public RoleType()
        {
            Field(x => x.Description, true);
            Field(x => x.Id);
            Field(x => x.Name);
            Field(x => x.NormalizedName);
            Field("permissions", x => x.Permissions.Select(x => x.Name).ToArray()).Description("Permissions in Role");
        }
    }
}
