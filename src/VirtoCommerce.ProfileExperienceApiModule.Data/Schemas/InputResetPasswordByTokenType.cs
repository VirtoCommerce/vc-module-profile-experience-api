using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputResetPasswordByTokenType : InputObjectGraphType
    {
        public InputResetPasswordByTokenType()
        {
            Field<NonNullGraphType<StringGraphType>>("token", "User password reset token");
            Field<NonNullGraphType<StringGraphType>>("userId", "User identifier");
            Field<NonNullGraphType<StringGraphType>>("newPassword", "New password according with system security policy");
        }
    }
}
