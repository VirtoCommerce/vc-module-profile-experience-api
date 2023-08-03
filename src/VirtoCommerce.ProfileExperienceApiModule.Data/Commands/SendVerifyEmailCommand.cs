using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendVerifyEmailCommand : ICommand<bool>
    {
        public SendVerifyEmailCommand()
        {
        }

        public SendVerifyEmailCommand(string storeId, string languageCode, string email, string userId)
        {
            StoreId = storeId;
            LanguageCode = languageCode;
            Email = email;
            UserId = userId;
        }

        public string UserId { get; set; }
        public string Email { get; set; }
        public string StoreId { get; set; }
        public string LanguageCode { get; set; }
    }
}
