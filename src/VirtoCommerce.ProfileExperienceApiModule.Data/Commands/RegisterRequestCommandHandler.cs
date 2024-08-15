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
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Models;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using CustomerSettings = VirtoCommerce.CustomerModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterRequestCommandHandler : IRequestHandler<RegisterRequestCommand, RegisterOrganizationResult>
    {
        private const string _userType = "Customer";

        private readonly IMapper _mapper;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        private readonly IMemberService _memberService;
        private readonly IStoreService _storeService;
        private readonly IAccountService _accountService;
        private readonly NewContactValidator _contactValidator;
        private readonly AccountValidator _accountValidator;
        private readonly AddressValidator _addressValidator;
        private readonly OrganizationValidator _organizationValidator;
        private readonly IOptions<FrontendSecurityOptions> _securityOptions;
        private readonly IMediator _mediator;

        protected Store CurrentStore { get; private set; }
        protected string EmailVerificationFlow { get; private set; }
        protected string DefaultContactStatus { get; private set; }
        protected string DefaultOrganizationStatus { get; private set; }
        protected Role MaintainerRole { get; private set; }

#pragma warning disable S107 // Method has x parameters which are greater than y
        public RegisterRequestCommandHandler(IMapper mapper,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            IMemberService memberService,
            IStoreService storeService,
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

            EmailVerificationFlow = CurrentStore.GetEmailVerificationFlow();

            // Read Settings
            DefaultContactStatus = CurrentStore.Settings.GetValue<string>(CustomerSettings.ContactDefaultStatus);

            DefaultOrganizationStatus = CurrentStore.Settings.GetValue<string>(CustomerSettings.OrganizationDefaultStatus);

            MaintainerRole = await GetMaintainerRole(result, tokenSource);
        }

        protected virtual async Task ProcessRequestAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            // Map incoming entities from request to Virto Commerce entities
            var requireAccountLock = EmailVerificationFlow == ModuleConstants.RegistrationFlows.EmailVerificationRequired;

            var account = ToApplicationUser(request.Account);

            var contact = await ToContact(request.Contact, account, request.LanguageCode, requireAccountLock);
            account.MemberId = contact.Id;

            Organization organization = null;
            if (request.Organization != null)
            {
                organization = await ToOrganization(request.Organization, contact, account);
                account.Roles = new List<Role> { MaintainerRole };
            }

            // Validate parameters & stop processing if any error is occurred
            var isValid = await ValidateAsync(organization, contact, request.Account, result, tokenSource);
            if (!isValid)
            {
                return;
            }

            // Create Organization
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
            // make user with confirmed email immediately if no email verification flow is seleced
            // other two flows allow user to confirm email
            account.EmailConfirmed = EmailVerificationFlow == ModuleConstants.RegistrationFlows.NoEmailVerification;
            // lock account before confirming email
            LockAccount(account, requireAccountLock);

            var identityResult = await _accountService.CreateAccountAsync(account);
            result.AccountCreationResult = ToAccountCreationResult(identityResult, account, requireAccountLock);

            if (!identityResult.Succeeded)
            {
                tokenSource.Cancel();
                return;
            }

            // Save account to result
            result.Contact.SecurityAccounts = new List<ApplicationUser> { account };

            // Send email notifications
            try
            {
                switch (EmailVerificationFlow)
                {
                    case ModuleConstants.RegistrationFlows.NoEmailVerification:
                        {
                            await SendRegistrationEmailNotificationAsync(request, result, tokenSource);
                            break;
                        }

                    case ModuleConstants.RegistrationFlows.EmailVerificationOptional:
                        {
                            await SendRegistrationEmailNotificationAsync(request, result, tokenSource);
                            await SendVerifyEmailCommandAsync(request, result, tokenSource);
                            break;
                        }
                    case ModuleConstants.RegistrationFlows.EmailVerificationRequired:
                        {
                            await SendVerifyEmailCommandAsync(request, result, tokenSource);
                            break;
                        }
                }
            }
            catch (Exception)
            {
                SetErrorResult(result, "NotificationError", "Cannot send registration notification", tokenSource);
            }
        }

        protected virtual void LockAccount(ApplicationUser account, bool requireAccountLock)
        {
            account.LockoutEnd = requireAccountLock ? DateTime.MaxValue : null;
        }

        protected virtual async Task<bool> ValidateAsync(Organization organization, Contact contact, Account account, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            var validationTasks = new List<Task<ValidationResult>>();

            validationTasks.AddRange(new[]{
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

        protected virtual async Task<Organization> ToOrganization(RegisteredOrganization organization, Contact contact, ApplicationUser account)
        {
            var result = _mapper.Map<Organization>(organization);

            result.Status = DefaultOrganizationStatus;
            result.OwnerId = contact.Id;
            result.Emails = new List<string> { ResolveEmail(account, result.Addresses ?? new List<Address>()) };

            await SetDynamicPropertiesAsync(organization.DynamicProperties, result);

            return result;
        }

        protected virtual async Task<Contact> ToContact(RegisteredContact contact, ApplicationUser account, string language, bool requireEmailVerification)
        {
            var result = _mapper.Map<Contact>(contact);

            result.Id = Guid.NewGuid().ToString();
            result.FullName = $"{contact.FirstName} {contact.LastName}";
            result.Name = result.FullName;
            result.Status = requireEmailVerification ? ModuleConstants.ContactStatuses.Locked : DefaultContactStatus;
            result.Emails = new List<string> { account.Email };
            result.DefaultLanguage = language;

            await SetDynamicPropertiesAsync(contact.DynamicProperties, result);

            return result;
        }

        protected virtual Task SendRegistrationEmailNotificationAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            var registrationNotificationRequest = new SendRegistrationNotificationCommand
            {
                Store = CurrentStore,
                LanguageCode = request.LanguageCode,
                Organization = result.Organization,
                Contact = result.Contact,
            };

            return _mediator.Send(registrationNotificationRequest, tokenSource.Token);
        }

        protected virtual Task SendVerifyEmailCommandAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            var sendVerifyEmailRequest = new SendVerifyEmailCommand(request.StoreId,
                request.LanguageCode,
                result.AccountCreationResult.Email,
                result.AccountCreationResult.AccountId);

            return _mediator.Send(sendVerifyEmailRequest, tokenSource.Token);
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
                SetErrorResult(result, "Role not found", $"The system could not find a role with the name '{maintainerRoleId}'. Create and configure a role with the name '{maintainerRoleId}'. Alternatively, change the role in the OrganizationMaintainerRole section to match an existing role.", tokenSource);
            }

            return role;
        }

        protected virtual AccountCreationResult ToAccountCreationResult(IdentityResult identityResult, ApplicationUser account, bool requireEmailVerification)
        {
            return new AccountCreationResult
            {
                Succeeded = identityResult.Succeeded,
                RequireEmailVerification = requireEmailVerification,
                AccountId = account.Id,
                AccountName = account.UserName,
                Email = account.Email,
                Errors = identityResult.Errors.Select(x => new RegistrationError
                {
                    Code = x.Code,
                    Description = x.Description,
                    Parameter = x is CustomIdentityError error ? error.Parameter : null,
                }).ToList(),
            };
        }

        protected virtual ApplicationUser ToApplicationUser(Account account) => new()
        {
            UserName = account.UserName,
            Email = account.Email,
            Password = account.Password,
            StoreId = CurrentStore.Id,
            Status = DefaultContactStatus,
            UserType = _userType,
        };

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

        protected static void SetErrorResult(RegisterOrganizationResult result, string errorCode, string errorMessage, CancellationTokenSource source)
        {
            SetErrorResult(result, new[] { new RegistrationError { Code = errorCode, Description = errorMessage } }, source);
        }

        protected static void SetErrorResult(RegisterOrganizationResult result, IList<RegistrationError> errors, CancellationTokenSource source)
        {
            result.AccountCreationResult ??= new AccountCreationResult();
            result.AccountCreationResult.Succeeded = false;
            result.AccountCreationResult.Errors ??= new List<RegistrationError>();
            result.AccountCreationResult.Errors.AddRange(errors);

            source.Cancel();
        }

        private static string ResolveEmail(ApplicationUser account, IList<Address> orgAddresses)
        {
            return orgAddresses.FirstOrDefault()?.Email ?? account.Email;
        }

        private Task SetDynamicPropertiesAsync(IList<DynamicPropertyValue> dynamicProperties, IHasDynamicProperties entity)
        {
            if (dynamicProperties?.Any() ?? false)
            {
                _dynamicPropertyUpdater.UpdateDynamicPropertyValues(entity, dynamicProperties);
            }

            return Task.CompletedTask;
        }
    }
}
