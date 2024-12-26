using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputResetPasswordByTokenType : ExtendableInputGraphType
    {
        public InputResetPasswordByTokenType()
        {
            Field<NonNullGraphType<StringGraphType>>("token", "User password reset token");
            Field<NonNullGraphType<StringGraphType>>("userId", "User identifier");
            Field<NonNullGraphType<StringGraphType>>("newPassword", "New password according with system security policy");
        }
    }
}
