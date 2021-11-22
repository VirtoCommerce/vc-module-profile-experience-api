using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputAssignPermissionScopeType : InputObjectGraphType<PermissionScope>
    {
        public InputAssignPermissionScopeType()
        {
            Field(x => x.Scope);
            Field(x => x.Type);
        }
    }
}
