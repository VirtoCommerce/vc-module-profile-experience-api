using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputSendVerifyEmailType : ExtendableInputGraphType
    {
        public InputSendVerifyEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId", "Store ID");
            Field<StringGraphType>("languageCode", "Notification language code");
            Field<StringGraphType>("email");
            Field<StringGraphType>("userId");
        }
    }
}
