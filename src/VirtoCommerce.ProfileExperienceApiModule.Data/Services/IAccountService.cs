using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services
{
    public interface IAccountService
    {
        public Task<IdentityResult> CreateAccountAsync(ApplicationUser account);
        public Task<ApplicationUser> GetAccountAsync(string userName);
        public Task<Role> FindRoleById(string roleId);
        public Task<Role> FindRoleByName(string roleName);
    }
}
