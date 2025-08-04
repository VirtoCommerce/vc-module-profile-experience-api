using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputAssignRoleType : ExtendableInputObjectGraphType<Role>
    {
        public InputAssignRoleType()
        {
            Field(x => x.ConcurrencyStamp, true);
            Field(x => x.Id);
            Field(x => x.Name);
            Field<NonNullGraphType<ListGraphType<InputAssignPermissionType>>>(nameof(Role.Permissions));
        }
    }
}
