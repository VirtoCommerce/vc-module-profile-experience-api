using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputChangeOrganizationContactRoleType : InputObjectGraphType
    {
        public InputChangeOrganizationContactRoleType()
        {
            Field<StringGraphType>(nameof(ChangeOrganizationContactRoleCommand.UserId)).Description("User identifier to be changed");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>(nameof(ChangeOrganizationContactRoleCommand.RoleIds)).Description("Role IDs or names to be assigned to the user");
        }
    }
}
