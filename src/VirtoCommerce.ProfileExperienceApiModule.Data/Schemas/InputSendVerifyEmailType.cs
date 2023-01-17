using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputSendVerifyEmailType : InputObjectGraphType
    {
        public InputSendVerifyEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId", "Store ID");
            Field<StringGraphType>("languageCode", "Notification language code");
            Field<StringGraphType>("email");
        }
    }
}
