using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputChangeOrganizationContactRoleType : ExtendableInputGraphType
    {
        public InputChangeOrganizationContactRoleType()
        {
            Field<StringGraphType>(nameof(ChangeOrganizationContactRoleCommand.UserId), "User identifier to be changed");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>(nameof(ChangeOrganizationContactRoleCommand.RoleIds), "Role IDs or names to be assigned to the user");
        }
    }
}
