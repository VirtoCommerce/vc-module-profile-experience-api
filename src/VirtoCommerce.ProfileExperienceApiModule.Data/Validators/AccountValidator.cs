using FluentValidation;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class AccountValidator : AbstractValidator<Account>
    {
        public AccountValidator()
        {
            RuleFor(x => x.UserName).NotNull().NotEmpty();
            RuleFor(x => x.Password).NotNull().NotEmpty();
            RuleFor(x => x.Email).NotNull().NotEmpty();
        }
    }
}
