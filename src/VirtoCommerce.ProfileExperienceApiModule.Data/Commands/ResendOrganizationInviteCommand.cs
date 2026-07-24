using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ResendOrganizationInviteCommand : ICommand<IdentityResultResponse>
    {
        public string MemberId { get; set; }

        public string OrganizationId { get; set; }

        public string UrlSuffix { get; set; }

        public string Message { get; set; }
    }
}
