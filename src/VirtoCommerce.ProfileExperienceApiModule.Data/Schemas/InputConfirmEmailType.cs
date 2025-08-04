using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputConfirmEmailType : ExtendableInputObjectGraphType
    {
        public InputConfirmEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId").Description("User identifier");
            Field<NonNullGraphType<StringGraphType>>("token").Description("Confirm email token");
        }
    }
}
