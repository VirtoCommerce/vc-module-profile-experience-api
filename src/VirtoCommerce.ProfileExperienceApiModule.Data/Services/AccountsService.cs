using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services
{
    public class AccountsService : IAccountService
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly IStoreNotificationSender _storeNotificationSender;

        public AccountsService(Func<UserManager<ApplicationUser>> userManagerFactory,
            IStoreNotificationSender storeNotificationSender)
        {
            _userManagerFactory = userManagerFactory;
            _storeNotificationSender = storeNotificationSender;
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

            if (result.Succeeded)
            {
                var user = await userManager.FindByNameAsync(account.UserName);

                await _storeNotificationSender.SendUserEmailVerificationAsync(user);
            }

            return result;
        }

        public async Task<ApplicationUser> GetAccountAsync(string userName)
        {
            using var userManager = _userManagerFactory();
            var account = await userManager.FindByNameAsync(userName);
            return account;
        }
    }
}
