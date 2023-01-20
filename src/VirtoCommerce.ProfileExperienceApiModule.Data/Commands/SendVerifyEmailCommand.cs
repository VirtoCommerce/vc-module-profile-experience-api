using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendVerifyEmailCommand : ICommand<bool>
    {
        public SendVerifyEmailCommand(string storeId, string languageCode, string email)
        {
            StoreId = storeId;
            LanguageCode = languageCode;
            Email = email;
        }

        public string Email { get; set; }
        public string StoreId { get; set; }
        public string LanguageCode { get; set; }
    }
}
