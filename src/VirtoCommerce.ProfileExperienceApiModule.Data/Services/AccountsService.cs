using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services
{
    public class AccountsService : IAccountService
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly Func<RoleManager<Role>> _roleManagerFactory;

        public AccountsService(Func<UserManager<ApplicationUser>> userManagerFactory,
            Func<RoleManager<Role>> roleManagerFactory)
        {
            _userManagerFactory = userManagerFactory;
            _roleManagerFactory = roleManagerFactory;
        }

        public async Task<IdentityResult> CreateAccountAsync(ApplicationUser account)
        {
            using var userManager = _userManagerFactory();

            var result = default(IdentityResult);

            if (account.Password.IsNullOrEmpty())
            {
                result = await userManager.CreateAsync(account);
            }
            else
            {
                result = await userManager.CreateAsync(account, account.Password);
            }

            return result;
        }

        public async Task<ApplicationUser> GetAccountAsync(string userName)
        {
            using var userManager = _userManagerFactory();
            var account = await userManager.FindByNameAsync(userName);
            return account;
        }

        public async Task<ApplicationUser> GetAccountByIdAsync(string id)
        {
            using var userManager = _userManagerFactory();
            return await userManager.FindByIdAsync(id);
        }

        public async Task<Role> FindRoleById(string roleId)
        {
            using var roleManager = _roleManagerFactory();
            return await roleManager.FindByIdAsync(roleId);
        }

        public async Task<Role> FindRoleByName(string roleName)
        {
            using var roleManager = _roleManagerFactory();
            return await roleManager.FindByNameAsync(roleName);
        }

        public async Task<IdentityResult> LockAccountByIdAsync(string id)
        {
            using var userManager = _userManagerFactory();

            var result = default(IdentityResult);

            var user = await userManager.FindByIdAsync(id);
            if (user != null)
            {
                result = await userManager.SetLockoutEndDateAsync(user, DateTime.MaxValue.ToUniversalTime());
            }

            return result;
        }

        public async Task<IdentityResult> UnlockAccountByIdAsync(string id)
        {
            using var userManager = _userManagerFactory();

            var result = default(IdentityResult);

            var user = await userManager.FindByIdAsync(id);
            if (user != null && user.LockoutEnd != null)
            {
                result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MinValue.ToUniversalTime());
            }

            return result;
        }

        public async Task<IdentityResult> DeleteAccountAsync(ApplicationUser account)
        {
            using var userManager = _userManagerFactory();
            return await userManager.DeleteAsync(account);
        }
    }
}
