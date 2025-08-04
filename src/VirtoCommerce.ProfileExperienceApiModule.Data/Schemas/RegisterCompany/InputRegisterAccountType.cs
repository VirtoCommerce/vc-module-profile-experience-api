using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRegisterAccountType : InputObjectGraphType<Account>
    {
        public InputRegisterAccountType()
        {
            Field<NonNullGraphType<StringGraphType>>("username");
            Field<NonNullGraphType<StringGraphType>>("email");
            Field<NonNullGraphType<StringGraphType>>("password");
        }
    }
}
