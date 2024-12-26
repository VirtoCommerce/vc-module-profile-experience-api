using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRequestRegistrationType : ExtendableInputGraphType
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
