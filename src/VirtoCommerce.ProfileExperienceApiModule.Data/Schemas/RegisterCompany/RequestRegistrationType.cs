using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RequestRegistrationType : ExtendableGraphType<RegisterOrganizationResult>
    {
        public RequestRegistrationType(IAccountService accountService)
        {
            Field<RegisterOrganizationType>("organization", "Created company", resolve: context => context.Source.Organization);
            Field<RegisterContactType>("contact", "Created contact", resolve: context => context.Source.Contact);
            Field<RegisterAccountType>("account", "Contact's account",
               resolve: context => context.Source.AccountCreationResult.Succeeded ?
                   accountService.GetAccountAsync(context.Source.AccountCreationResult.AccountName) : null);
            Field<AccountCreationResultType>("result", "Account creation result", resolve: context => context.Source.AccountCreationResult);
        }
    }
}
