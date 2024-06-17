using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ConfirmEmailCommand : ICommand<IdentityResultResponse>
    {
        public string UserId { get; set; }
        public string Token { get; set; }
    }
}
