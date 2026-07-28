using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputRevokeOrganizationInviteType : ExtendableInputObjectGraphType
    {
        public InputRevokeOrganizationInviteType()
        {
            Field<NonNullGraphType<StringGraphType>>("MemberId").Description("Contact member ID");
        }
    }
}
