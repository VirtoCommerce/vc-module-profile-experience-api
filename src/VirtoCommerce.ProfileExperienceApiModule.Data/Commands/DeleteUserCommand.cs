using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class DeleteUserCommand : ICommand<IdentityResult>
    {
        public string[] UserNames { get; set; }
        public DeleteUserCommand(string[] userNames)
        {
            UserNames = userNames;
        }
    }
}
