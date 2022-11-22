using FluentValidation;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator()
        {
            RuleFor(x => x.Email)
                .Must(x => string.IsNullOrEmpty(x) || x.IsValidEmail())
                .WithMessage("Invalid email format an the address");
        }
    }
}
