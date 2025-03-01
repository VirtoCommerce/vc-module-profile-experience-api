using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class AccountCreationResultType : ExtendableGraphType<AccountCreationResult>
    {
        public AccountCreationResultType()
        {
            Field(x => x.Succeeded);
            Field(x => x.RequireEmailVerification);
            Field<ListGraphType<RegistrationErrorType>>("errors").Description("The errors that occurred during the operation.").Resolve(context => context.Source.Errors);
        }
    }
}
