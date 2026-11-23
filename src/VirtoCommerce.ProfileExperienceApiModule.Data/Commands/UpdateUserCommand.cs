using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateUserCommand : ICommand<IdentityResult>
    {
        public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
    }
}
