using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Notifications;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Models;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.StoreModule.Core.Model;
using CustomerCore = VirtoCommerce.CustomerModule.Core;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterRequestCommandHandler : IRequestHandler<RegisterRequestCommand, RegisterOrganizationResult>
    {
        private readonly IMapper _mapper;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        private readonly IMemberService _memberService;
        private readonly ICrudService<Store> _storeService;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;
        private readonly IAccountService _accountService;
        private readonly NewContactValidator _contactValidator;
        private readonly AccountValidator _accountValidator;
        private readonly AddressValidator _addressValidator;
        private readonly OrganizationValidator _organizationValidator;
        private readonly IOptions<FrontendSecurityOptions> _securityOptions;

        private const string Creator = "frontend";
        private const string UserType = "Manager";
#pragma warning disable S107
        public RegisterRequestCommandHandler(IMapper mapper,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            IMemberService memberService,
            ICrudService<Store> storeService,
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender,
            IAccountService accountService,
            NewContactValidator contactValidator,
            AccountValidator accountValidator,
            AddressValidator addressValidator,
            OrganizationValidator organizationValidator,
            IOptions<FrontendSecurityOptions> securityOptions)
#pragma warning restore S107
        {
            _mapper = mapper;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _memberService = memberService;
            _storeService = storeService;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
            _accountService = accountService;
            _contactValidator = contactValidator;
            _accountValidator = accountValidator;
            _addressValidator = addressValidator;
            _organizationValidator = organizationValidator;
            _securityOptions = securityOptions;
        }

        public virtual async Task<RegisterOrganizationResult> Handle(RegisterRequestCommand request, CancellationToken cancellationToken)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var internalToken = cancellationTokenSource.Token;

            var result = await ProcessRequestAsync(request, cancellationTokenSource);

            if (internalToken.IsCancellationRequested)
            {
                await RollBackMembersCreationAsync(result);
            }

            return result;
        }

        private async Task<RegisterOrganizationResult> ProcessRequestAsync(RegisterRequestCommand request, CancellationTokenSource tokenSource)
        {
            var result = new RegisterOrganizationResult();
            IList<Role> roles = null;

            var organization = _mapper.Map<Organization>(request.Organization);
            var contact = _mapper.Map<Contact>(request.Contact);
            var account = GetApplicationUser(request.Account);

            FillContactFields(contact);

            var validationTasks = new List<Task<ValidationResult>>
            {
                _contactValidator.ValidateAsync(contact),
                _accountValidator.ValidateAsync(request.Account),
                _organizationValidator.ValidateAsync(organization)
            };

            foreach (var address in organization?.Addresses ?? new List<Address>())
            {
                validationTasks.Add(_addressValidator.ValidateAsync(address));
            }

            var validationResults = await Task.WhenAll(validationTasks);

            if (validationResults.Any(x => !x.IsValid))
            {
                var errors = validationResults
                    .SelectMany(x => x.Errors)
                    .Select(x => new RegistrationError { Code = x.ErrorCode, Description = x.ErrorMessage })
                    .ToList();

                SetErrorResult(result, errors, tokenSource);
                return result;
            }

            await SetDynamicPropertiesAsync(request.Contact.DynamicProperties, contact);

            var store = await _storeService.GetByIdAsync(request.StoreId);

            if (store == null)
            {
                SetErrorResult(result, "Store not found", $"Store {request.StoreId} has not been found", tokenSource);
                return result;
            }

            var maintainerRole = await GetMaintainerRole(result, tokenSource);
            if (maintainerRole == null)
            {
                return result;
            }

            roles = new List<Role> { maintainerRole };

            await SetDynamicPropertiesAsync(request.Organization.DynamicProperties, organization);
            var organizationStatus = store
                .Settings
                .GetSettingValue<string>(CustomerCore.ModuleConstants.Settings.General.OrganizationDefaultStatus.Name, null);
            organization.CreatedBy = Creator;
            organization.Status = organizationStatus;
            organization.OwnerId = contact.Id;
            organization.Emails = new List<string> { organization.Addresses?.FirstOrDefault()?.Email ?? account.Email };

            await _memberService.SaveChangesAsync(new Member[] { organization });

            result.Organization = organization;

            var contactStatus = store.Settings
                .GetSettingValue<string>(CustomerCore.ModuleConstants.Settings.General.ContactDefaultStatus.Name, null);

            contact.Status = contactStatus;
            contact.CreatedBy = Creator;
            contact.Organizations = new List<string> { organization.Id };
            contact.Emails = new List<string> { account.Email };
            await _memberService.SaveChangesAsync(new Member[] { contact });
            result.Contact = contact;

            account.StoreId = request.StoreId;
            account.Status = contactStatus;
            account.UserType = UserType;
            account.MemberId = contact.Id;
            account.Roles = roles;
            account.CreatedBy = Creator;
            result.Contact.SecurityAccounts = new List<ApplicationUser> { account };

            var identityResult = await _accountService.CreateAccountAsync(account);
            result.AccountCreationResult = GetAccountCreationResult(identityResult, account);

            if (!result.AccountCreationResult.Succeeded)
            {
                tokenSource.Cancel();
                return result;
            }

            var notificationRequest = new RegisterOrganizationNotificationRequest
            {
                Organization = organization,
                Contact = contact,
                Store = store,
                LanguageCode = request.LanguageCode
            };

            await SendNotificationAsync(notificationRequest);
            return result;
        }

        private async Task<Role> GetMaintainerRole(RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            var maintainerRoleId = _securityOptions.Value.OrganizationMaintainerRole;
            if (maintainerRoleId == null)
            {
                SetErrorResult(result, "Role not configured", "Organization maintainer role configuration is not found in the app settings", tokenSource);
                return null;
            }

            var role = await _accountService.FindRoleByName(maintainerRoleId) ?? await _accountService.FindRoleById(maintainerRoleId);
            if (role == null)
            {
                SetErrorResult(result, "Role not found", $"Organization maintainer role {maintainerRoleId} not found", tokenSource);
            }

            return role;
        }

        private static AccountCreationResult GetAccountCreationResult(IdentityResult identityResult, ApplicationUser account)
        {
            return new AccountCreationResult
            {
                Succeeded = identityResult.Succeeded,
                AccountName = account.UserName,
                Errors = identityResult.Errors.Select(x => new RegistrationError
                {
                    Code = x.Code,
                    Description = x.Description,
                    Parameter = x is CustomIdentityError error ? error.Parameter.ToString() : null
                }).ToList()
            };
        }

        private static void FillContactFields(Contact contact)
        {
            contact.FullName = contact.FirstName + " " + contact.LastName;
            contact.Name = contact.FullName;
            contact.Id = Guid.NewGuid().ToString();
        }

        private async Task RollBackMembersCreationAsync(RegisterOrganizationResult result)
        {
            var ids = new[] { result.Organization?.Id, result.Contact?.Id }
                .Where(x => x != null)
                .ToArray();
            await _memberService.DeleteAsync(ids);

            result.Organization = null;
            result.Contact = null;
        }

        private Task SetDynamicPropertiesAsync(IList<DynamicPropertyValue> dynamicProperties, IHasDynamicProperties entity)
        {
            if (dynamicProperties?.Any() ?? false)
            {
                _dynamicPropertyUpdater.UpdateDynamicPropertyValues(entity, dynamicProperties);
            }

            return Task.CompletedTask;
        }

        private static void SetErrorResult(RegisterOrganizationResult result, string errorCode, string errorMessage, CancellationTokenSource source)
        {
            SetErrorResult(result, new List<RegistrationError> { new() { Code = errorCode, Description = errorMessage } }, source);
        }

        private static void SetErrorResult(RegisterOrganizationResult result, List<RegistrationError> errors, CancellationTokenSource source)
        {
            result.AccountCreationResult = new AccountCreationResult
            {
                Succeeded = false,
                Errors = errors
            };

            source.Cancel();
        }

        protected virtual async Task SendNotificationAsync(RegisterOrganizationNotificationRequest request)
        {
            var notification = request.Organization != null
                ? await GetRegisterCompanyNotificationAsync(request)
                : await GetRegisterContactNotificationAsync(request);

            notification.From = request.Store.Email;
            notification.LanguageCode = request.LanguageCode;

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }

        protected virtual async Task<EmailNotification> GetRegisterCompanyNotificationAsync(RegisterOrganizationNotificationRequest request)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegisterCompanyEmailNotification>(new TenantIdentity(request.Store.Id, nameof(Store)));
            notification.To = request.Organization.Emails.FirstOrDefault();
            notification.CompanyName = request.Organization.Name;
            return notification;
        }

        protected virtual async Task<EmailNotification> GetRegisterContactNotificationAsync(RegisterOrganizationNotificationRequest request)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegistrationEmailNotification>(new TenantIdentity(request.Store.Id, nameof(Store)));
            notification.To = request.Contact.Emails.FirstOrDefault();
            notification.FirstName = request.Contact.FirstName;
            notification.LastName = request.Contact.LastName;
            notification.Login = request.Contact.SecurityAccounts.FirstOrDefault()?.UserName;
            return notification;
        }

        protected virtual ApplicationUser GetApplicationUser(Account account) => new()
        {
            UserName = account.UserName,
            Email = account.Email,
            Password = account.Password
        };
    }
}
