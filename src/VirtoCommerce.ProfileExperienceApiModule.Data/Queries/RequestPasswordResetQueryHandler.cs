using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class RequestPasswordResetQueryHandler : IQueryHandler<RequestPasswordResetQuery, bool>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;
        private readonly IStoreService _storeService;

        public RequestPasswordResetQueryHandler(
            Func<UserManager<ApplicationUser>> userManagerFactory,
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender,
            IStoreService storeService)
        {
            _userManagerFactory = userManagerFactory;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
            _storeService = storeService;
        }

        public virtual async Task<bool> Handle(RequestPasswordResetQuery request, CancellationToken cancellationToken)
        {
            using var userManager = _userManagerFactory();

            var user = await userManager.FindByNameAsync(request.LoginOrEmail)
                       ?? await userManager.FindByEmailAsync(request.LoginOrEmail);

            if (user != null &&
                !string.IsNullOrEmpty(user.Email) &&
                !string.IsNullOrEmpty(user.StoreId) &&
                (user.LockoutEnd is null || user.LockoutEnd < DateTime.UtcNow))
            {
                var store = await _storeService.GetByIdAsync(user.StoreId);

                if (!string.IsNullOrEmpty(store?.Url) && !string.IsNullOrEmpty(store.Email))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);

                    var notification = await _notificationSearchService.GetNotificationAsync<ResetPasswordEmailNotification>(new TenantIdentity(store.Id, nameof(Store)));
                    notification.Url = $"{store.Url.TrimLastSlash()}{request.UrlSuffix.NormalizeUrlSuffix()}?userId={user.Id}&token={Uri.EscapeDataString(token)}";
                    notification.To = user.Email;
                    notification.From = store.Email;

                    await _notificationSender.ScheduleSendNotificationAsync(notification);
                }
            }

            return true;
        }
    }
}
