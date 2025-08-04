using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRequestRegistrationType : ExtendableInputObjectGraphType
    {
        public InputRequestRegistrationType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId").Description("Store ID");
            Field<StringGraphType>("languageCode").Description("Notification language code");
            Field<InputRegisterOrganizationType>("organization").Description("company type");
            Field<NonNullGraphType<InputRegisterContactType>>("contact").Description("Creating contact");
            Field<NonNullGraphType<InputRegisterAccountType>>("account").Description("Creating contact's account");
        }
    }
}
