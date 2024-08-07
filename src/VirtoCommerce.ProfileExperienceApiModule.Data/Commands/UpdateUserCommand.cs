using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateUserCommand : ICommand<IdentityResult>
    {
        public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
    }
}
