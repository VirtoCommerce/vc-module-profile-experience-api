using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, IdentityResultResponse>
    {
        private readonly IWebHostEnvironment _environment;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly Func<RoleManager<Role>> _roleManagerFactory;
        private readonly IMemberService _memberService;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;
        private readonly IStoreService _storeService;

        public InviteUserCommandHandler(
            IWebHostEnvironment environment,
            Func<UserManager<ApplicationUser>> userManager, IMemberService memberService,
            INotificationSearchService notificationSearchService, INotificationSender notificationSender,
            IStoreService storeService, Func<RoleManager<Role>> roleManagerFactory)
        {
            _environment = environment;
            _userManagerFactory = userManager;
            _memberService = memberService;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
            _storeService = storeService;
            _roleManagerFactory = roleManagerFactory;
        }

        public virtual async Task<IdentityResultResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
        {
            var result = new IdentityResultResponse
            {
                Errors = new List<IdentityErrorInfo>(),
                Succeeded = false,
            };

            // PT-6083: reduce complexity
            foreach (var email in request.Emails.Distinct())
            {
                using var userManager = _userManagerFactory();

                var contact = new Contact { FirstName = string.Empty, LastName = string.Empty, FullName = string.Empty, Organizations = new List<string> { request.OrganizationId } };
                await _memberService.SaveChangesAsync(new Member[] { contact });

                var user = new ApplicationUser { UserName = email, Email = email, MemberId = contact.Id, StoreId = request.StoreId };
                var identityResult = await userManager.CreateAsync(user);
                
                if (identityResult.Succeeded)
                {
                    var store = await _storeService.GetByIdAsync(user.StoreId);
                    if (store == null)
                    {
                        var errors = _environment.IsDevelopment() ? new[] { new IdentityError { Code = "StoreNotFound", Description = "Store not found" } } : null;
                        identityResult = IdentityResult.Failed(errors);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(store.Url) || string.IsNullOrEmpty(store.Email))
                        {
                            var errors = _environment.IsDevelopment() ? new[] { new IdentityError { Code = "StoreNotConfigured", Description = "Store has invalid URL or email" } } : null;
                            identityResult = IdentityResult.Failed(errors);
                        }
                        else
                        {
                            result.Errors.AddRange(await AssignUserRoles(user, request.RoleIds));
                            await SendNotificationAsync(request, store, email);
                        }
                    }
                }

                result.Errors.AddRange(identityResult.Errors.Select(x => x.MapToIdentityErrorInfo()));
                result.Succeeded |= identityResult.Succeeded;

                if (!result.Succeeded)
                {
                    await _memberService.DeleteAsync(new[] { contact.Id });

                    if (user.Id != null)
                    {
                        await userManager.DeleteAsync(user);
                    }
                }
            }

            return result;
        }

        protected virtual async Task<List<IdentityErrorInfo>> AssignUserRoles(ApplicationUser user, string[] roleIds)
        {
            var errors = new List<IdentityErrorInfo>();
            var roles = new List<Role>();

            if (roleIds.IsNullOrEmpty())
            {
                return errors;
            }

            using var roleManager = _roleManagerFactory();
            using var userManager = _userManagerFactory();

            foreach (var roleId in roleIds)
            {
                var role = await roleManager.FindByIdAsync(roleId) ?? await roleManager.FindByNameAsync(roleId);
                if (role != null)
                {
                    roles.Add(role);
                }
                else
                {
                    errors.Add(new IdentityErrorInfo{Code = "Role not found", Description = $"Role '{roleId}' not found", Parameter = roleId});
                }
            }
            
            var assignResult = await userManager.AddToRolesAsync(user, roles.Select(x => x.NormalizedName).ToArray());
            errors.AddRange(assignResult.Errors.Select(x => x.MapToIdentityErrorInfo()));

            return errors;
        }

        protected virtual async Task SendNotificationAsync(InviteUserCommand request, Store store, string email)
        {
            using var userManager = _userManagerFactory();

            var user = await userManager.FindByEmailAsync(email);
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var notification = await _notificationSearchService.GetNotificationAsync<RegistrationInvitationEmailNotification>();
            notification.InviteUrl = $"{store.Url.TrimLastSlash()}{request.UrlSuffix.NormalizeUrlSuffix()}?userId={user.Id}&email={user.Email}&token={Uri.EscapeDataString(token)}";
            notification.Message = request.Message;
            notification.To = user.Email;
            notification.From = store.Email;

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }
    }
}
