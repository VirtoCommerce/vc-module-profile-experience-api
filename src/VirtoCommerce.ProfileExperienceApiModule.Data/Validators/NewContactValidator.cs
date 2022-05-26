using FluentValidation;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class NewContactValidator : AbstractValidator<Contact>
    {
        public NewContactValidator()
        {
            RuleFor(x => x.FirstName).NotNull().NotEmpty();
            RuleFor(x => x.FullName).NotNull();
            RuleFor(x => x.LastName).NotNull().NotEmpty();
            RuleFor(x => x.Name).NotNull();

            RuleSet("strict", () =>
            {
                RuleFor(x => x).Custom((member, context) =>
                {
                    if (member.Addresses.IsNullOrEmpty())
                    {
                        context.AddFailure(ErrorDescriber.AddressesMissingError(member));
                    }
                });
            });
        }
    }
}
