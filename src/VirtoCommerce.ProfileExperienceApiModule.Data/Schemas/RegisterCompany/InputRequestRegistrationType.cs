using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRequestRegistrationType : InputObjectGraphType
    {
        public InputRequestRegistrationType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId", "Store ID");
            Field<StringGraphType>("languageCode", "Notification language code");
            Field<InputRegisterOrganizationType>("organization", "company type");
            Field<NonNullGraphType<InputRegisterContactType>>("contact", "Creating contact");
            Field<NonNullGraphType<InputRegisterAccountType>>("account", "Creating contact's account");
        }
    }
}
