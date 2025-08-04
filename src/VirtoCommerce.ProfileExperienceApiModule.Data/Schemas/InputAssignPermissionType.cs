using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputAssignPermissionType : ExtendableInputObjectGraphType<Permission>
    {
        public InputAssignPermissionType()
        {
            Field<ListGraphType<InputAssignPermissionScopeType>>(nameof(Permission.AssignedScopes));
            Field(x => x.Name);
        }
    }
}
