using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class PasswordValidationQuery : IQuery<IdentityResultResponse>
    {
        public string Password { get; set; }
    }
}
