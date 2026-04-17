using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ChangePasswordCommand : ICommand<IdentityResultResponse>
    {
        public string UserId { get; set; }

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }

        public string SessionGroupId { get; set; }
    }
}
