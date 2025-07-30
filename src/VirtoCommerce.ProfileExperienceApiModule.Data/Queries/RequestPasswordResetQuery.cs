using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class RequestPasswordResetQuery : IQuery<bool>
    {
        public string StoreId { get; set; }
        public string CultureName { get; set; }
        public string LoginOrEmail { get; set; }

        public string UrlSuffix { get; set; }
    }
}
