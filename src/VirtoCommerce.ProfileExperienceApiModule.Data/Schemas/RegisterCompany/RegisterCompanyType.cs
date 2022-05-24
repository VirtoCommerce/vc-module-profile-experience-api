using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterCompanyType : ExtendableGraphType<RegisterCompanyResult>
    {
        public RegisterCompanyType(IAccountService accountService)
        {
            Field<CompanyType>("company", "Created company", resolve: context => context.Source.Company);
            Field<OwnerType>("contact", "Created contact", resolve: context => context.Source.Owner);
            Field<AccountType>("account", "Contact's account",
               resolve: context => context.Source.AccountCreationResult.Succeeded ?
                   accountService.GetAccountAsync(context.Source.AccountCreationResult.AccountName) : null);
            Field<AccountCreationResultType>("result", "Account creation result", resolve: context => context.Source.AccountCreationResult);
        }
    }
}
