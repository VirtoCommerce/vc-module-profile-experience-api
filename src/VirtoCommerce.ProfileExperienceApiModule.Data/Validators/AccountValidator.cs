using FluentValidation;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class AccountValidator : AbstractValidator<Account>
    {
        public AccountValidator()
        {
            RuleFor(x => x.UserName).NotNull().NotEmpty();
            RuleFor(x => x.Password).NotNull().NotEmpty();
            RuleFor(x => x.Email)
                .NotNull()
                .NotEmpty()
                .Must(x => x.IsValidEmail())
                .WithMessage("Invalid email format");
        }
    }
}
