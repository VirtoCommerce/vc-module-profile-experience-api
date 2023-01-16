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
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendVerifyEmailCommandHandler : IRequestHandler<SendVerifyEmailCommand, bool>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly ICrudService<Store> _storeService;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        public SendVerifyEmailCommandHandler(Func<UserManager<ApplicationUser>> userManagerFactory,
            ICrudService<Store> storeService,
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
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user == null || user.StoreId != request.StoreId)
                {
                    return true;
                }

                var store = await _storeService.GetByIdAsync(request.StoreId);
                if (store == null)
                {
                    return true;
                }

                var settingDescriptor = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General.EmailVerificationEnabled;

                if (store.Settings.GetSettingValue(settingDescriptor.Name, (bool)settingDescriptor.DefaultValue))
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
