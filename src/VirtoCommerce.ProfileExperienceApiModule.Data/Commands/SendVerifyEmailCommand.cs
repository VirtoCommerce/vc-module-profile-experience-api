using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendVerifyEmailCommand : ICommand<bool>
    {
        public SendVerifyEmailCommand(string email)
        {
            Email = email;
        }

        public string Email { get; set; }
    }
}
