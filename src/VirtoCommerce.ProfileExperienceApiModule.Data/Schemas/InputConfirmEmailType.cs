using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputConfirmEmailType : InputObjectGraphType
    {
        public InputConfirmEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId", "User identifier");
            Field<NonNullGraphType<StringGraphType>>("token", "Confirm email token");
        }
    }
}
