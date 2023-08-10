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
        private const string UserType = "Manager";

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
        private readonly IMediator _mediator;

        protected Store CurrentStore { get; private set; }
        protected string DefaultContactStatus { get; private set; }
        protected string DefaultOrganizationStatus { get; private set; }
        protected Role MaintainerRole { get; private set; }

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
            IOptions<FrontendSecurityOptions> securityOptions,
            IMediator mediator)
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
            _mediator = mediator;
        }

        public virtual async Task<RegisterOrganizationResult> Handle(RegisterRequestCommand request, CancellationToken cancellationToken)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var internalToken = cancellationTokenSource.Token;

            var result = new RegisterOrganizationResult();

            await BeforeProcessRequestAsync(request, result, cancellationTokenSource);
            if (internalToken.IsCancellationRequested)
            {
                return result;
            }

            await ProcessRequestAsync(request, result, cancellationTokenSource);

            await AfterProcessRequestAsync(request, result, cancellationTokenSource);

            if (internalToken.IsCancellationRequested)
            {
                await RollBackMembersCreationAsync(result);
            }

            return result;
        }

        protected virtual Task AfterProcessRequestAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            return Task.CompletedTask;
        }

        protected virtual async Task BeforeProcessRequestAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            // Resolve Current Store
            CurrentStore = await _storeService.GetByIdAsync(request.StoreId);

            if (CurrentStore == null)
            {
                SetErrorResult(result, "Store not found", $"Store {request.StoreId} has not been found", tokenSource);
                return;
            }

            // Read Settings
            DefaultContactStatus = CurrentStore.Settings
                .GetSettingValue<string>(CustomerCore.ModuleConstants.Settings.General.ContactDefaultStatus.Name, null);

            DefaultOrganizationStatus = CurrentStore.Settings
                .GetSettingValue<string>(CustomerCore.ModuleConstants.Settings.General.OrganizationDefaultStatus.Name, null);

            MaintainerRole = await GetMaintainerRole(result, tokenSource);
        }

#pragma warning disable S138
        protected virtual async Task ProcessRequestAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            // Map incoming enties from request to Virto Commerce enties
            var account = ToApplicationUser(request.Account);

            var contact = await ToContact(request.Contact, account);
            account.MemberId = contact.Id;

            Organization organization = null;
            if (request.Organization != null)
            {
                organization = await ToOrganization(request.Organization, contact, account);
                account.Roles = new List<Role> { MaintainerRole };
            }

            // Validate parameters & stop processing if any error is occured
            var isValid = await ValidateAsync(organization, contact, request.Account, result, tokenSource);
            if (!isValid)
            {
                return;
            }

            // Create Organisation
            if (organization != null)
            {
                await _memberService.SaveChangesAsync(new Member[] { organization });

                // Create relation between contact and organization
                contact.Organizations = new List<string> { organization.Id };
            }

            // Create Contact
            await _memberService.SaveChangesAsync(new Member[] { contact });

            // Save contact/org to result
            result.Organization = organization;
            result.Contact = contact;

            // Create Security Account
            var identityResult = await _accountService.CreateAccountAsync(account);
            result.AccountCreationResult = ToAccountCreationResult(identityResult, account);

            if (!identityResult.Succeeded)
            {
                tokenSource.Cancel();
                return;
            }

            // Save account to result
            result.Contact.SecurityAccounts = new List<ApplicationUser> { account };

            // Send Notifications
            var notificationRequest = new RegisterOrganizationNotificationRequest
            {
                Store = CurrentStore,
                LanguageCode = request.LanguageCode,
                Organization = organization,
                Contact = contact,
            };

            try
            {
                await SendRegistrationEmailNotificationAsync(notificationRequest);

                // Send Email Verification Command
                await SendVerifyEmailCommand(request, account.Email, tokenSource);
            }
            catch (Exception)
            {
                tokenSource.Cancel();

                result.AccountCreationResult.Succeeded = false;
                result.AccountCreationResult.Errors = new List<RegistrationError>
                {
                    new RegistrationError
                    {
                        Code = "NotificationError",
                        Description = "Cannot send registration notification",
                    }
                };
            }
        }

        private async Task<bool> ValidateAsync(Organization organization, Contact contact, Account account, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            var validationTasks = new List<Task<ValidationResult>>();

            validationTasks.AddRange(new Task<ValidationResult>[]{
                _organizationValidator.ValidateAsync(organization),
                _contactValidator.ValidateAsync(contact),
                _accountValidator.ValidateAsync(account)});

            var orgAddresses = organization?.Addresses ?? new List<Address>();
            foreach (var address in orgAddresses)
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
                return false;
            }

            return true;
        }

        private async Task<Organization> ToOrganization(RegisteredOrganization organization, Contact contact, ApplicationUser account)
        {
            var result = _mapper.Map<Organization>(organization);

            result.Status = DefaultOrganizationStatus;
            result.OwnerId = contact.Id;
            result.Emails = new List<string> { ResolveEmail(account, result.Addresses ?? new List<Address>()) };

            await SetDynamicPropertiesAsync(organization.DynamicProperties, result);


            return result;
        }

        private async Task<Contact> ToContact(RegisteredContact contact, ApplicationUser account)
        {
            var result = _mapper.Map<Contact>(contact);

            result.Id = Guid.NewGuid().ToString();
            result.FullName = contact.FirstName + " " + contact.LastName;
            result.Name = result.FullName;
            result.Status = DefaultContactStatus;
            result.Emails = new List<string> { account.Email };

            await SetDynamicPropertiesAsync(contact.DynamicProperties, result);

            return result;
        }

        protected virtual async Task SendRegistrationEmailNotificationAsync(RegisterOrganizationNotificationRequest request)
        {
            var notification = request.Organization != null
                ? await GetRegisterCompanyNotificationAsync(request)
                : await GetRegisterContactNotificationAsync(request);

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }

        protected virtual async Task<EmailNotification> GetRegisterCompanyNotificationAsync(RegisterOrganizationNotificationRequest request)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegisterCompanyEmailNotification>(new TenantIdentity(request.Store.Id, nameof(Store)));

            notification.From = request.Store.Email;
            notification.LanguageCode = string.IsNullOrEmpty(request.LanguageCode) ? request.Store.DefaultLanguage : request.LanguageCode;

            notification.To = request.Organization.Emails.FirstOrDefault();
            notification.CompanyName = request.Organization.Name;

            return notification;
        }

        protected virtual async Task<EmailNotification> GetRegisterContactNotificationAsync(RegisterOrganizationNotificationRequest request)
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

        protected virtual Task SendVerifyEmailCommand(RegisterRequestCommand request, string email, CancellationTokenSource tokenSource)
        {
            return _mediator.Send(new SendVerifyEmailCommand(request.StoreId,
                request.LanguageCode,
                email), tokenSource.Token);
        }

        private static string ResolveEmail(ApplicationUser account, IList<Address> orgAdresses)
        {
            return orgAdresses.FirstOrDefault()?.Email ?? account.Email;
        }

        protected virtual async Task<Role> GetMaintainerRole(RegisterOrganizationResult result, CancellationTokenSource tokenSource)
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

        protected virtual AccountCreationResult ToAccountCreationResult(IdentityResult identityResult, ApplicationUser account)
        {
            return new AccountCreationResult
            {
                Succeeded = identityResult.Succeeded,
                AccountId = account.Id,
                AccountName = account.UserName,
                Errors = identityResult.Errors.Select(x => new RegistrationError
                {
                    Code = x.Code,
                    Description = x.Description,
                    Parameter = x is CustomIdentityError error ? error.Parameter.ToString() : null
                }).ToList()
            };
        }

        protected virtual ApplicationUser ToApplicationUser(Account account) => new()
        {
            UserName = account.UserName,
            Email = account.Email,
            Password = account.Password,
            StoreId = CurrentStore.Id,
            Status = DefaultContactStatus,
            UserType = UserType,
        };

#pragma warning restore S138

        protected virtual async Task RollBackMembersCreationAsync(RegisterOrganizationResult result)
        {
            var ids = new[] { result.Organization?.Id, result.Contact?.Id }
                .Where(x => x != null)
                .ToArray();
            await _memberService.DeleteAsync(ids);

            result.Organization = null;
            result.Contact = null;

            if (result.AccountCreationResult?.AccountId != null)
            {
                var account = await _accountService.GetAccountByIdAsync(result.AccountCreationResult.AccountId);
                if (account != null)
                {
                    await _accountService.DeleteAccountAsync(account);
                }

                result.AccountCreationResult.AccountId = null;
                result.AccountCreationResult.AccountName = null;
            }
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

    }
}
