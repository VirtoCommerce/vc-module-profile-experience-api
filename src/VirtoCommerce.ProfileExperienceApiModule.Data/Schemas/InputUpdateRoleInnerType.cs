using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateRoleInnerType : ExtendableInputObjectGraphType
    {
        public InputUpdateRoleInnerType()
        {
            Field<StringGraphType>("concurrencyStamp").Description("Concurrency Stamp");
            Field<NonNullGraphType<StringGraphType>>("id").Description("Role ID");
            Field<NonNullGraphType<StringGraphType>>("name").Description("Role name");
            Field<StringGraphType>("description").Description("Role description");
            Field<NonNullGraphType<ListGraphType<InputAssignPermissionType>>>(nameof(Role.Permissions)).Description("List of Role permissions");
        }
    }
}
