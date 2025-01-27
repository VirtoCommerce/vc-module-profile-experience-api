using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputConfirmEmailType : InputObjectGraphType
    {
        public InputConfirmEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId").Description("User identifier");
            Field<NonNullGraphType<StringGraphType>>("token").Description("Confirm email token");
        }
    }
}
