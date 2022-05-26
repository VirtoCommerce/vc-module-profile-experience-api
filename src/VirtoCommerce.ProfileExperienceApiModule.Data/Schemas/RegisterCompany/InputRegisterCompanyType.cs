using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRegisterCompanyType : InputObjectGraphType
    {
        public InputRegisterCompanyType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId", "Store ID");
            Field<InputCompanyType>("company", "company type");
            Field<NonNullGraphType<InputContactType>>("contact", "Creating contact");
            Field<NonNullGraphType<InputAccountType>>("account", "Creating contact's account");
        }
    }
}
