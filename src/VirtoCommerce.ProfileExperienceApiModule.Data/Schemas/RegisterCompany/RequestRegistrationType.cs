using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RequestRegistrationType : ExtendableGraphType<RegisterOrganizationResult>
    {
        public RequestRegistrationType(IAccountService accountService)
        {
            Field<RegisterOrganizationType>("organization").Description("Created company").Resolve(context => context.Source.Organization);
            Field<RegisterContactType>("contact").Description("Created contact").Resolve(context => context.Source.Contact);
            Field<RegisterAccountType>("account").Description("Contact's account")
               .ResolveAsync(async context => context.Source.AccountCreationResult.Succeeded ?
                   await accountService.GetAccountAsync(context.Source.AccountCreationResult.AccountName) : null);
            Field<AccountCreationResultType>("result").Description("Account creation result").Resolve(context => context.Source.AccountCreationResult);
        }
    }
}
