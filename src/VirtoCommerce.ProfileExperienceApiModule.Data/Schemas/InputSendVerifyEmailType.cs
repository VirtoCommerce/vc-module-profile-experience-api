using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputSendVerifyEmailType : InputObjectGraphType
    {
        public InputSendVerifyEmailType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId").Description("Store ID");
            Field<StringGraphType>("languageCode").Description("Notification language code");
            Field<StringGraphType>("email");
            Field<StringGraphType>("userId");
        }
    }
}
