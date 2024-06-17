using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ResetPasswordByTokenCommand : ICommand<IdentityResultResponse>
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public string NewPassword { get; set; }
    }
}
