using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterRequestCommand : ICommand<RegisterOrganizationResult>
    {
        public string StoreId { get; set; }
        public string LanguageCode { get; set; }
        public RegisteredOrganization Organization { get; set; }
        public RegisteredContact Contact { get; set; }
        public Account Account { get; set; }
    }
}
