using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RequestRegistrationType : ExtendableGraphType<RegisterOrganizationResult>
    {
        public RequestRegistrationType(IAccountService accountService)
        {
            Field<RegisterOrganizationType>("organization", "Created company", resolve: context => context.Source.Organization);
            Field<RegisterContactType>("contact", "Created contact", resolve: context => context.Source.Contact);
            FieldAsync<RegisterAccountType>("account", "Contact's account",
               resolve: async context => context.Source.AccountCreationResult.Succeeded ?
                   await accountService.GetAccountAsync(context.Source.AccountCreationResult.AccountName) : null);
            Field<AccountCreationResultType>("result", "Account creation result", resolve: context => context.Source.AccountCreationResult);
        }
    }
}
