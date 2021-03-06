using FluentValidation;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class UserQueryValidator : AbstractValidator<GetUserQuery>
    {
        public UserQueryValidator()
        {
            RuleFor(x => x).Must(x =>
                !x.Id.IsNullOrEmpty() ^
                !x.Email.IsNullOrEmpty() ^
                !x.UserName.IsNullOrEmpty() ^
                !(x.LoginProvider.IsNullOrEmpty() && x.ProviderKey.IsNullOrEmpty()))
                .WithMessage("Only one of {Id, Email, UserName, {LoginProvider AND ProviderKey}} must be specified!");
        }
    }
}
