using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputConfirmEmailType : ExtendableInputGraphType
    {
        public InputConfirmEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("userId", "User identifier");
            Field<NonNullGraphType<StringGraphType>>("token", "Confirm email token");
        }
    }
}
