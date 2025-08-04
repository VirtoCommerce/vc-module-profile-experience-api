using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputChangePasswordType : ExtendableInputObjectGraphType
    {
        public InputChangePasswordType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId").Description("User identifier");
            Field<NonNullGraphType<StringGraphType>>("oldPassword").Description("Old user password");
            Field<NonNullGraphType<StringGraphType>>("newPassword").Description("New password according with system security policy");
        }
    }
}
