using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputChangeOrganizationContactRoleType : ExtendableInputObjectGraphType
    {
        public InputChangeOrganizationContactRoleType()
        {
            Field<NonNullGraphType<StringGraphType>>(nameof(ChangeOrganizationContactRoleCommand.MemberId)).Description("Contact member ID to be changed");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>(nameof(ChangeOrganizationContactRoleCommand.RoleIds)).Description("Role IDs or names to be assigned to the user within the organization");
        }
    }
}
