using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRegisterCompanyType : InputObjectGraphType
    {
        public InputRegisterCompanyType()
        {
            Field<NonNullGraphType<StringGraphType>>("storeId", "Store ID");
            Field<NonNullGraphType<InputCompanyType>>("company", "company type");
            Field<NonNullGraphType<InputOwnerType>>("owner", "company owner");
            Field<NonNullGraphType<InputAccountType>>("account", "company owner account");
        }
    }
}
