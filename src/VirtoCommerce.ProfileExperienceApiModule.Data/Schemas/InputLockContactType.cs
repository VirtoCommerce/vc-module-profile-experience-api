using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputLockUnlockOrganizationContactType : ExtendableInputObjectGraphType
    {
        public InputLockUnlockOrganizationContactType()
        {
            Field<NonNullGraphType<StringGraphType>>("MemberId").Description("Contact member ID");
        }
    }
}
