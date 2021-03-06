using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateRoleInnerType : InputObjectGraphType
    {
        public InputUpdateRoleInnerType()
        {
            Field<StringGraphType>("concurrencyStamp", description: "Concurrency Stamp");
            Field<NonNullGraphType<StringGraphType>>("id", description: "Role ID");
            Field<NonNullGraphType<StringGraphType>>("name", description: "Role name");
            Field<StringGraphType>("description", description: "Role description");
            Field<NonNullGraphType<ListGraphType<InputAssignPermissionType>>>(nameof(Role.Permissions), description: "List of Role permissions");
        }
    }
}
