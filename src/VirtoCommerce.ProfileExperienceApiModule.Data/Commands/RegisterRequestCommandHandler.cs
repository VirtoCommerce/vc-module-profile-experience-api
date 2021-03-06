using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation.Results;
using MediatR;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Models;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Notifications;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

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
        private readonly OrganizationValidator _organizationValidator;

        private const string Creator = "frontend";
        private const string UserType = "Manager";
        private const string MaintainerRoleId = "org-maintainer";
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
            OrganizationValidator organizationValidator)
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
            _organizationValidator = organizationValidator;
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

            var validationResults = await Task.WhenAll(validationTasks);

            if (validationResults.Any(x => !x.IsValid))
            {
                var errors = validationResults
                    .SelectMany(x => x.Errors)
                    .Select(x => $"{x.ErrorCode}: {x.ErrorMessage}".TrimEnd(' ', ':'))
                    .ToList();

                SetErrorResult(result, errors, tokenSource);
                return result;
            }

            await SetDynamicPropertiesAsync(request.Contact.DynamicProperties, contact);

            var store = await _storeService.GetByIdAsync(request.StoreId);

            if (store == null)
            {
                SetErrorResult(result, $"Store {request.StoreId} has not been found", tokenSource);
                return result;
            }

            if (organization != null)
            {
                var maintainerRole = await _accountService.FindRoleById(MaintainerRoleId);
                if (maintainerRole == null)
                {
                    SetErrorResult(result, $"Organization maintainer role with id {MaintainerRoleId} not found", tokenSource);
                    return result;
                }

                roles = new List<Role> { maintainerRole };

                await SetDynamicPropertiesAsync(request.Organization.DynamicProperties, organization);
                var organizationStatus = store
                    .Settings
                    .GetSettingValue<string>(ModuleConstants.Settings.General.OrganizationDefaultStatus.Name, null);
                organization.CreatedBy = Creator;
                organization.Status = organizationStatus;
                organization.OwnerId = contact.Id;
                organization.Emails = new List<string> { organization.Addresses?.FirstOrDefault()?.Email ?? account.Email };

                await _memberService.SaveChangesAsync(new Member[] { organization });

                result.Organization = organization;
            }

            var contactStatus = store.Settings
                .GetSettingValue<string>(ModuleConstants.Settings.General.ContactDefaultStatus.Name, null);

            contact.Status = contactStatus;
            contact.CreatedBy = Creator;
            contact.Organizations = organization != null ? new List<string> { organization.Id } : null;
            contact.Emails = new List<string> { account.Email };
            await _memberService.SaveChangesAsync(new Member[] { contact });
            result.Contact = contact;

            account.StoreId = request.StoreId;
            account.Status = contactStatus;
            account.UserType = UserType;
            account.MemberId = contact.Id;
            account.Roles = roles;
            account.CreatedBy = Creator;

            var identityResult = await _accountService.CreateAccountAsync(account);
            result.AccountCreationResult = new AccountCreationResult
            {
                Succeeded = identityResult.Succeeded,
                Errors = identityResult.Errors
                    .Select(x => $"{x.Code}: {x.Description}".TrimEnd(' ', ':'))
                    .ToList(),
                AccountName = account.UserName
            };

            if (!result.AccountCreationResult.Succeeded)
            {
                tokenSource.Cancel();
                return result;
            }

            await SendNotificationAsync(result, store);
            return result;
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

        private static void SetErrorResult(RegisterOrganizationResult result, string errorMessage, CancellationTokenSource source)
        {
            SetErrorResult(result, new List<string> { errorMessage }, source);
        }

        private static void SetErrorResult(RegisterOrganizationResult result, List<string> errors, CancellationTokenSource source)
        {
            result.AccountCreationResult = new AccountCreationResult
            {
                Succeeded = false,
                Errors = errors
            };

            source.Cancel();
        }

        protected virtual async Task SendNotificationAsync(RegisterOrganizationResult result, Store store)
        {
            var notification = result.Organization != null
                ? await GetRegisterCompanyNotificationAsync(result, store)
                : await GetRegisterContactNotificationAsync(result, store);

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }

        protected virtual async Task<EmailNotification> GetRegisterCompanyNotificationAsync(RegisterOrganizationResult result, Store store)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegisterCompanyEmailNotification>();
            notification.To = result.Organization.Emails.FirstOrDefault();
            notification.From = store.Email;
            notification.CompanyName = result.Organization.Name;
            notification.LanguageCode = store.DefaultLanguage;
            return notification;
        }

        protected virtual async Task<EmailNotification> GetRegisterContactNotificationAsync(RegisterOrganizationResult result, Store store)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegistrationEmailNotification>();
            notification.To = result.Contact.Emails.FirstOrDefault();
            notification.From = store.Email;
            notification.FirstName = result.Contact.FirstName;
            notification.LastName = result.Contact.LastName;
            notification.Login = result.AccountCreationResult.AccountName;
            notification.LanguageCode = store.DefaultLanguage;
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
