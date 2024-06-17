using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class InviteUserCommand : ICommand<IdentityResultResponse>
    {
        public string StoreId { get; set; }

        public string OrganizationId { get; set; }

        public string UrlSuffix { get; set; }

        public string[] Emails { get; set; }

        public string Message { get; set; }

        public string[] RoleIds { get; set; }

        public string CustomerOrderId { get; set; }
    }
}
