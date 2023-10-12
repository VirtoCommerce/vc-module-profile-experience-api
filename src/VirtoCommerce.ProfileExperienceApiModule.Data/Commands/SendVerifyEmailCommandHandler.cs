using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using StoreSettings = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendVerifyEmailCommandHandler : IRequestHandler<SendVerifyEmailCommand, bool>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly IStoreService _storeService;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        public SendVerifyEmailCommandHandler(Func<UserManager<ApplicationUser>> userManagerFactory,
            IStoreService storeService,
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender)
        {
            _userManagerFactory = userManagerFactory;
            _storeService = storeService;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
        }

        public virtual async Task<bool> Handle(SendVerifyEmailCommand request, CancellationToken cancellationToken)
        {
            using (var userManager = _userManagerFactory())
            {
                ApplicationUser user = null;

                if (!string.IsNullOrEmpty(request.UserId))
                {
                    user = await userManager.FindByIdAsync(request.UserId);
                }
                else if (!string.IsNullOrEmpty(request.Email))
                {
                    user = await userManager.FindByEmailAsync(request.Email);
                }

                if (user == null || user.StoreId != request.StoreId)
                {
                    return true;
                }

                var store = await _storeService.GetByIdAsync(request.StoreId);
                if (store == null)
                {
                    return true;
                }

                if (store.Settings.GetValue<bool>(StoreSettings.EmailVerificationEnabled))
                {
                    await SendConfirmationEmailNotificationAsync(store, user, request.LanguageCode);
                }

                return true;
            }
        }

        protected virtual async Task SendConfirmationEmailNotificationAsync(Store store, ApplicationUser user, string languageCode)
        {
            var emailVerificationNotification = await GetConfirmationEmailNotificationAsync(store, user, languageCode);

            await _notificationSender.ScheduleSendNotificationAsync(emailVerificationNotification);
        }

        protected virtual async Task<EmailNotification> GetConfirmationEmailNotificationAsync(Store store, ApplicationUser user, string languageCode)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<ConfirmationEmailNotification>(new TenantIdentity(store.Id, nameof(Store)));

            notification.To = user.Email;
            notification.Url = await GenerateEmailVerificationLink(store, user);
            notification.From = store.Email;
            notification.LanguageCode = string.IsNullOrEmpty(languageCode) ? store.DefaultLanguage : languageCode;

            return notification;
        }

        protected virtual async Task<string> GenerateEmailVerificationLink(Store store, ApplicationUser user)
        {
            if (store.Url.IsNullOrEmpty() || !Uri.IsWellFormedUriString(store.Url, UriKind.Absolute))
            {
                throw new OperationCanceledException($"A valid URL is required in Url property for store '{store.Id}'.");
            }

            using var userManager = _userManagerFactory();
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            return QueryHelpers.AddQueryString(new Uri($"{store.Url.TrimEnd('/')}/account/confirmemail").ToString(),
                    new Dictionary<string, string>
                    {
                    { "UserId", user.Id },
                    { "Token", token }
                });
        }
    }
}
