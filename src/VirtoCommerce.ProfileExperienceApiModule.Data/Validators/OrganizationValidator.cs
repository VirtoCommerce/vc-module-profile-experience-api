using FluentValidation;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class OrganizationValidator:  AbstractValidator<Organization>
    {
        public OrganizationValidator()
        {
            RuleFor(x => x.Name).NotNull().NotEmpty();
        }
    }
}
