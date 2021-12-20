using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetUserQuery : IQuery<ApplicationUser>
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string LoginProvider { get; }
        public string ProviderKey { get; }
        public string UserId { get; set; }

        public GetUserQuery()
        {

        }

        public GetUserQuery(string id, string email, string userName, string loginProvider, string providerKey)
        {
            Id = id;
            Email = email;
            UserName = userName;
            LoginProvider = loginProvider;
            ProviderKey = providerKey;
        }
    }
}
