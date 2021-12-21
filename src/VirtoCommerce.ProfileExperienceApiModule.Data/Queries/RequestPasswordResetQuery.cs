using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class RequestPasswordResetQuery : IQuery<bool>
    {
        public string LoginOrEmail { get; set; }

        public string UrlSuffix { get; set; }
    }
}
