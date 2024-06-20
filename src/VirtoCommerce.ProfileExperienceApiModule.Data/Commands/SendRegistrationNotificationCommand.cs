using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendRegistrationNotificationCommand : ICommand<bool>
    {
        public Contact Contact { get; set; }
        public Organization Organization { get; set; }
        public Store Store { get; set; }
        public string LanguageCode { get; set; }
    }
}
