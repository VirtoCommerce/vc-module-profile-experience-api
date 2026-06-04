using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        private const string _userType = "Customer";

        private readonly IWebHostEnvironment _environment;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly Func<RoleManager<Role>> _roleManagerFactory;
        private readonly IMemberService _memberService;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;
        private readonly IStoreService _storeService;
        private readonly IOrganizationMembershipService _organizationMembershipService;

        public InviteUserCommandHandler(
            IWebHostEnvironment environment,
            Func<UserManager<ApplicationUser>> userManager,
            IMemberService memberService,
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender,
            IStoreService storeService,
            Func<RoleManager<Role>> roleManagerFactory,
            IOrganizationMembershipService organizationMembershipService)
        {
            _environment = environment;
            _userManagerFactory = userManager;
            _memberService = memberService;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
            _storeService = storeService;
            _roleManagerFactory = roleManagerFactory;
            _organizationMembershipService = organizationMembershipService;
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

                var contact = CreateContact(request, email);

                await _memberService.SaveChangesAsync(new Member[] { contact });

                var user = CreateUser(request, contact, email);
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
                            result.Errors.AddRange(await AssignUserRoles(user, request.RoleIds, request.OrganizationId));
                            await SendNotificationAsync(request, store, email);
                        }
                    }
                }

                result.Errors.AddRange(identityResult.Errors.Select(x => x.MapToIdentityErrorInfo()));
                result.Succeeded |= identityResult.Succeeded;

                if (!identityResult.Succeeded)
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

        protected virtual Contact CreateContact(InviteUserCommand request, string email)
        {
            var contact = AbstractTypeFactory<Contact>.TryCreateInstance();
            contact.Status = ModuleConstants.ContactStatuses.Invited;
            contact.FirstName = string.Empty;
            contact.LastName = string.Empty;
            contact.FullName = string.Empty;
            contact.Emails = new List<string> { email };

            if (!string.IsNullOrEmpty(request.OrganizationId))
            {
                contact.Organizations = new List<string> { request.OrganizationId };
            }

            return contact;
        }

        protected virtual ApplicationUser CreateUser(InviteUserCommand request, Contact contact, string email)
        {
            var user = AbstractTypeFactory<ApplicationUser>.TryCreateInstance();

            user.UserName = email;
            user.Email = email;
            user.MemberId = contact.Id;
            user.StoreId = request.StoreId;
            user.UserType = _userType;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            return user;
        }

        protected virtual async Task<List<IdentityErrorInfo>> AssignUserRoles(ApplicationUser user, string[] roleIds, string organizationId)
        {
            var errors = new List<IdentityErrorInfo>();

            if (roleIds.IsNullOrEmpty())
            {
                return errors;
            }

            using var roleManager = _roleManagerFactory();

            var roles = new List<Role>();
            foreach (var roleId in roleIds)
            {
                var role = await roleManager.FindByIdAsync(roleId) ?? await roleManager.FindByNameAsync(roleId);
                if (role != null)
                {
                    roles.Add(role);
                }
                else
                {
                    errors.Add(new IdentityErrorInfo { Code = "Role not found", Description = $"Role '{roleId}' not found", Parameter = roleId });
                }
            }

            if (errors.Count > 0)
            {
                return errors;
            }

            if (!string.IsNullOrEmpty(organizationId))
            {
                var membership = new OrganizationMembership
                {
                    UserId = user.Id,
                    OrganizationId = organizationId,
                    Roles = roles
                        .Select(r => new OrganizationMembershipRole
                        {
                            RoleId = r.Id,
                            RoleName = r.Name,
                        })
                        .ToList(),
                };
                await _organizationMembershipService.SaveChangesAsync([membership]);
            }
            else
            {
                using var userManager = _userManagerFactory();
                var assignResult = await userManager.AddToRolesAsync(user, roles.Select(x => x.NormalizedName).ToArray());
                errors.AddRange(assignResult.Errors.Select(x => x.MapToIdentityErrorInfo()));
            }

            return errors;
        }

        protected virtual async Task SendNotificationAsync(InviteUserCommand request, Store store, string email)
        {
            using var userManager = _userManagerFactory();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return;
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // take notification
            RegistrationInvitationNotificationBase notification = !string.IsNullOrEmpty(request.OrganizationId)
                ? await _notificationSearchService.GetNotificationAsync<RegistrationInvitationEmailNotification>(new TenantIdentity(store.Id, nameof(Store)))
                : await _notificationSearchService.GetNotificationAsync<RegistrationInvitationCustomerEmailNotification>(new TenantIdentity(store.Id, nameof(Store)));

            notification.InviteUrl = $"{store.Url.TrimLastSlash()}{request.UrlSuffix.NormalizeUrlSuffix()}?userId={user.Id}&email={HttpUtility.UrlEncode(user.Email)}&token={Uri.EscapeDataString(token)}";

            if (!string.IsNullOrEmpty(request.CustomerOrderId))
            {
                notification.InviteUrl = $"{notification.InviteUrl}&customerOrderId={request.CustomerOrderId}";
            }

            notification.Message = request.Message;
            notification.To = user.Email;
            notification.From = store.Email;

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }
    }
}
