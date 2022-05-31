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
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.NotificationsModule.Core.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterCompanyCommandHandler : IRequestHandler<RegisterCompanyCommand, RegisterCompanyResult>
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
        public RegisterCompanyCommandHandler(IMapper mapper,
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

        public virtual async Task<RegisterCompanyResult> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
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

        private async Task<RegisterCompanyResult> ProcessRequestAsync(RegisterCompanyCommand request, CancellationTokenSource tokenSource)
        {
            var result = new RegisterCompanyResult();
            IList<Role> roles = null;

            var company = _mapper.Map<Organization>(request.Company);
            var contact = _mapper.Map<Contact>(request.Contact);
            var account = GetApplicationUser(request.Account);
            
            FillContactFields(contact);

            var validationTasks = new List<Task<ValidationResult>>
            {
                _contactValidator.ValidateAsync(contact),
                _accountValidator.ValidateAsync(request.Account),
                _organizationValidator.ValidateAsync(company)
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

            if (company != null)
            {
                var maintainerRole = await _accountService.FindRoleById(MaintainerRoleId);
                if (maintainerRole == null)
                {
                    SetErrorResult(result, $"Organization maintainer role with id {MaintainerRoleId} not found", tokenSource);
                    return result;
                }

                roles = new List<Role> { maintainerRole };

                await SetDynamicPropertiesAsync(request.Company.DynamicProperties, company);
                var organizationStatus = store
                    .Settings
                    .GetSettingValue<string>(ModuleConstants.Settings.General.OrganizationDefaultStatus.Name, null);
                company.CreatedBy = Creator;
                company.Status = organizationStatus;
                company.OwnerId = contact.Id;
                company.Emails = new List<string> { company.Addresses.FirstOrDefault()?.Email };

                await _memberService.SaveChangesAsync(new Member[] { company });

                result.Company = company;
            }

            var contactStatus = store.Settings
                .GetSettingValue<string>(ModuleConstants.Settings.General.ContactDefaultStatus.Name, null);

            contact.Status = contactStatus;
            contact.CreatedBy = Creator;
            contact.Organizations = company != null ? new List<string> { company.Id } : null;
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

        private async Task RollBackMembersCreationAsync(RegisterCompanyResult result)
        {
            var ids = new[] { result.Company?.Id, result.Contact?.Id }
                .Where(x => x != null)
                .ToArray();
            await _memberService.DeleteAsync(ids);

            result.Company = null;
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

        private static void SetErrorResult(RegisterCompanyResult result, string errorMessage, CancellationTokenSource source)
        {
            SetErrorResult(result, new List<string>{errorMessage}, source);
        }

        private static void SetErrorResult(RegisterCompanyResult result, List<string> errors, CancellationTokenSource source)
        {
            result.AccountCreationResult = new AccountCreationResult
            {
                Succeeded = false,
                Errors = errors
            };

            source.Cancel();
        }

        protected virtual async Task SendNotificationAsync(RegisterCompanyResult result, Store store)
        {
            if (result.Company != null)
            {
                var registerCompanyNotification = await _notificationSearchService.GetNotificationAsync<RegisterCompanyEmailNotification>();
                registerCompanyNotification.To = result.Company.Emails.FirstOrDefault();
                registerCompanyNotification.From = store.Email;
                registerCompanyNotification.CompanyName = result.Company.Name;
                registerCompanyNotification.LanguageCode = store.DefaultLanguage;

                await _notificationSender.ScheduleSendNotificationAsync(registerCompanyNotification);
            }
            else
            {
                var registerContactNotification = await _notificationSearchService.GetNotificationAsync<RegistrationEmailNotification>();
                registerContactNotification.To = result.Contact.Emails.FirstOrDefault();
                registerContactNotification.From = store.Email;
                registerContactNotification.FirstName = result.Contact.FirstName;
                registerContactNotification.LastName = result.Contact.LastName;
                registerContactNotification.Login = result.AccountCreationResult.AccountName;
                registerContactNotification.LanguageCode = store.DefaultLanguage;

                await _notificationSender.ScheduleSendNotificationAsync(registerContactNotification);
            }
        }

        protected virtual ApplicationUser GetApplicationUser(Account account) => new()
        {
            UserName = account.UserName, Email = account.Email, Password = account.Password
        };
    }
}
