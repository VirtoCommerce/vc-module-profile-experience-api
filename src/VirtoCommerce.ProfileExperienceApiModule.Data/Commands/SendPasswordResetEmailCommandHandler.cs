using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class SendPasswordResetEmailCommandHandler : IRequestHandler<SendPasswordResetEmailCommand, bool>
{
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
    private readonly INotificationSearchService _notificationSearchService;
    private readonly INotificationSender _notificationSender;
    private readonly IStoreService _storeService;

    public SendPasswordResetEmailCommandHandler(
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

    public async Task<bool> Handle(SendPasswordResetEmailCommand request, CancellationToken cancellationToken)
    {
        using var userManager = _userManagerFactory();

        var user = await userManager.FindByNameAsync(request.LoginOrEmail)
                   ?? await userManager.FindByEmailAsync(request.LoginOrEmail);

        if (user == null)
        {
            return true;
        }

        var storeId = request.StoreId ?? user.StoreId;

        if ((user.LockoutEnd != null && DateTime.UtcNow < user.LockoutEnd) ||
            user.Email.IsNullOrEmpty() ||
            storeId.IsNullOrEmpty())
        {
            return true;
        }

        var store = await _storeService.GetByIdAsync(storeId);

        if (store == null ||
            store.Url.IsNullOrEmpty() ||
            store.Email.IsNullOrEmpty())
        {
            return true;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var notification = await _notificationSearchService.GetNotificationAsync<ResetPasswordEmailNotification>(new TenantIdentity(storeId, nameof(Store)));

        notification.Url = $"{store.Url.TrimLastSlash()}{request.UrlSuffix.NormalizeUrlSuffix()}?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        notification.To = user.Email;
        notification.From = store.Email;
        notification.LanguageCode = request.CultureName ?? store.DefaultLanguage;

        await _notificationSender.ScheduleSendNotificationAsync(notification);

        return true;
    }
}
