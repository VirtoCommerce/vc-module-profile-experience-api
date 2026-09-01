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
            Field<StringGraphType>(nameof(ChangeOrganizationContactRoleCommand.StoreId)).Description("ID of store whose company-role whitelist should be used for validation. When omitted, the member's own store is used");
            Field<ListGraphType<NonNullGraphType<StringGraphType>>>(nameof(ChangeOrganizationContactRoleCommand.RoleIds)).Description("Role IDs or names to be assigned to the user within the organization");
        }
    }
}
