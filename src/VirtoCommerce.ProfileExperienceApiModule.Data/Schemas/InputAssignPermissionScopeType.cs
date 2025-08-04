using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputAssignPermissionScopeType : ExtendableInputObjectGraphType<PermissionScope>
    {
        public InputAssignPermissionScopeType()
        {
            Field(x => x.Scope);
            Field(x => x.Type);
        }
    }
}
