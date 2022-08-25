using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegistrationErrorType : ObjectGraphType<RegistrationError>
    {
        public RegistrationErrorType()
        {
            Field(x => x.Code, true);
            Field(x => x.Description, true);
            Field(x => x.Parameter, true);
        }
    }
}
