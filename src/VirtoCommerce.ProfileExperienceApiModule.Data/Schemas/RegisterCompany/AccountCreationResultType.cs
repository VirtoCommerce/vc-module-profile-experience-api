using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class AccountCreationResultType: ObjectGraphType<AccountCreationResult>
    {
        public AccountCreationResultType()
        {
            Field(x => x.Succeeded);
            Field<ListGraphType<RegistrationErrorType>>("errors", "The errors that occurred during the operation.", resolve: context => context.Source.Errors);
        }
    }
}
