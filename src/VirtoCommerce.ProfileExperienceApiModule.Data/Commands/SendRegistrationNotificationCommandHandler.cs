using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Notifications;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SendRegistrationNotificationCommandHandler : IRequestHandler<SendRegistrationNotificationCommand, bool>
    {
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        public SendRegistrationNotificationCommandHandler(INotificationSearchService notificationSearchService, INotificationSender notificationSender)
        {
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
        }

        public async Task<bool> Handle(SendRegistrationNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = request.Organization != null
                ? await GetRegisterCompanyNotificationAsync(request)
                : await GetRegisterContactNotificationAsync(request);

            await _notificationSender.ScheduleSendNotificationAsync(notification);
            return true;
        }

        protected virtual async Task<EmailNotification> GetRegisterCompanyNotificationAsync(SendRegistrationNotificationCommand request)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegisterCompanyEmailNotification>(new TenantIdentity(request.Store.Id, nameof(Store)));

            notification.From = request.Store.Email;
            notification.LanguageCode = string.IsNullOrEmpty(request.LanguageCode) ? request.Store.DefaultLanguage : request.LanguageCode;

            notification.To = request.Organization.Emails.FirstOrDefault();
            notification.CompanyName = request.Organization.Name;

            return notification;
        }

        protected virtual async Task<EmailNotification> GetRegisterContactNotificationAsync(SendRegistrationNotificationCommand request)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegistrationEmailNotification>(new TenantIdentity(request.Store.Id, nameof(Store)));

            notification.From = request.Store.Email;
            notification.LanguageCode = string.IsNullOrEmpty(request.LanguageCode) ? request.Store.DefaultLanguage : request.LanguageCode;

            notification.To = request.Contact.Emails.FirstOrDefault();
            notification.FirstName = request.Contact.FirstName;
            notification.LastName = request.Contact.LastName;
            notification.Login = request.Contact.SecurityAccounts.FirstOrDefault()?.UserName;

            return notification;
        }
    }
}
