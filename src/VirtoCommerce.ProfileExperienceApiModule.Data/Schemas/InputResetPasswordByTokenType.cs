using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputResetPasswordByTokenType : InputObjectGraphType
    {
        public InputResetPasswordByTokenType()
        {
            Field<NonNullGraphType<StringGraphType>>("token").Description("User password reset token");
            Field<NonNullGraphType<StringGraphType>>("userId").Description("User identifier");
            Field<NonNullGraphType<StringGraphType>>("newPassword").Description("New password according with system security policy");
        }
    }
}
