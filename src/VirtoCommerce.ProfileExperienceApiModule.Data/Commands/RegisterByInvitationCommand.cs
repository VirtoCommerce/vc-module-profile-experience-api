using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterByInvitationCommand : ICommand<IdentityResultResponse>
    {
        public string UserId { get; set; }

        public string Token { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string CustomerOrderId { get; set; }
    }
}
