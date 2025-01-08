using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateRoleInnerType : InputObjectGraphType
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
