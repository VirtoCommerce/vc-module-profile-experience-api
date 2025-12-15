using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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

        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly Func<RoleManager<Role>> _roleManagerFactory;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        private readonly IInviteCustomerService _inviteCustomerService;

        public InviteUserCommandHandler(IInviteCustomerService inviteCustomerService)
        {
            _inviteCustomerService = inviteCustomerService;
        }

        [Obsolete("Obsolete constructor. Use the constructor with IInviteCustomerService.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        private InviteUserCommandHandler(
            IWebHostEnvironment environment,
            Func<UserManager<ApplicationUser>> userManager, IMemberService memberService,
            INotificationSearchService notificationSearchService, INotificationSender notificationSender,
            IStoreService storeService, Func<RoleManager<Role>> roleManagerFactory)
        {
            _userManagerFactory = userManager;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
            _roleManagerFactory = roleManagerFactory;
        }

        public virtual async Task<IdentityResultResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
        {
            var inviteCustomerRequest = new InviteCustomerRequest
            {
                StoreId = request.StoreId,
                OrganizationId = request.OrganizationId,
                RoleIds = request.RoleIds,
                Emails = request.Emails,
                Message = request.Message,
                UrlSuffix = request.UrlSuffix,
            };

            if (!request.CustomerOrderId.IsNullOrEmpty())
            {
                inviteCustomerRequest.AdditionalParameters.Add("customerOrderId", request.CustomerOrderId);
            }

            var result = await _inviteCustomerService.InviteCustomerAsyc(inviteCustomerRequest, cancellationToken);

            return new IdentityResultResponse
            {
                Succeeded = result.Succeeded,
                Errors = result.Errors.Select(x => new IdentityErrorInfo
                {
                    Code = x.Code,
                    Description = x.Description,
                    Parameter = x.Parameter
                }).ToList()
            };
        }

        [Obsolete("Not being called. Override CreateContact in InviteCustomerService.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
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

        [Obsolete("Not being called. Override CreateUser in InviteCustomerService.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
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

        [Obsolete("Not being called. Override AssignUserRoles in InviteCustomerService.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
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
                    errors.Add(new IdentityErrorInfo { Code = "Role not found", Description = $"Role '{roleId}' not found", Parameter = roleId });
                }
            }

            var assignResult = await userManager.AddToRolesAsync(user, roles.Select(x => x.NormalizedName).ToArray());
            errors.AddRange(assignResult.Errors.Select(x => x.MapToIdentityErrorInfo()));

            return errors;
        }

        [Obsolete("Not being called. Override SendNotificationAsync in InviteCustomerService.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions/")]
        protected virtual async Task SendNotificationAsync(InviteUserCommand request, Store store, string email)
        {
            using var userManager = _userManagerFactory();

            var user = await userManager.FindByEmailAsync(email);
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
