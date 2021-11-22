using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputAssignPermissionType : InputObjectGraphType<Permission>
    {
        public InputAssignPermissionType()
        {
            Field<ListGraphType<InputAssignPermissionScopeType>>(nameof(Permission.AssignedScopes));
            Field(x => x.Name);
        }
    }
}
