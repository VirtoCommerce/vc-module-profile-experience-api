using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterCompanyType : ExtendableGraphType<RegisterCompanyAggregate>
    {
        public RegisterCompanyType()
        {
            Field<CompanyType>("company", "Created company", resolve: context => context.Source.Company);
            Field<OwnerType>("owner", "Company owner", resolve: context => context.Source.Owner);
            Field<AccountType>("account", "Company owner's account", resolve: context => context.Source.Account);
            Field<IdentityResultType>("result", "Account creation result", resolve: context => context.Source.AccountCreationResult);
        }
    }
}
