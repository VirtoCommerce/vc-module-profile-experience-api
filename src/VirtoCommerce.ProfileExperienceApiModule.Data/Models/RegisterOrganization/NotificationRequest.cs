using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization
{
    public class NotificationRequest
    {
        

        public Contact Contact { get; set; }
        public Organization Organization { get; set; }
        public Store Store { get; set; }
        public string LanguageCode { get; set; }
    }
}
