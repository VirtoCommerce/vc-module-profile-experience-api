using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services
{
    public interface IAccountService
    {
        public Task<IdentityResult> CreateAccountAsync(ApplicationUser account);
        public Task<ApplicationUser> GetAccountAsync(string userName);
        public Task<ApplicationUser> GetAccountByIdAsync(string id);
        public Task<Role> FindRoleById(string roleId);
        public Task<Role> FindRoleByName(string roleName);
        public Task<IdentityResult> LockAccountByIdAsync(string id);
        public Task<IdentityResult> UnlockAccountByIdAsync(string id);
        public Task<IdentityResult> DeleteAccountAsync(ApplicationUser account);
    }
}
