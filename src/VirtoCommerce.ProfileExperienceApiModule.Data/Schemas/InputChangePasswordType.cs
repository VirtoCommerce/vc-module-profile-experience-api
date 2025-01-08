using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputChangePasswordType : InputObjectGraphType
    {
        public InputChangePasswordType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId").Description("User identifier");
            Field<NonNullGraphType<StringGraphType>>("oldPassword").Description("Old user password");
            Field<NonNullGraphType<StringGraphType>>("newPassword").Description("New password according with system security policy");
        }
    }
}
