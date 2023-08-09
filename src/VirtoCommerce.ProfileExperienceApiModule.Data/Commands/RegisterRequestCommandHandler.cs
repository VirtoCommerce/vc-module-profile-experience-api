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
using RegistrationFlows = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants.RegistrationFlows;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterRequestCommandHandler : IRequestHandler<RegisterRequestCommand, RegisterOrganizationResult>
    {
        private const string _userType = "Manager";

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

            // Read Settings
            DefaultContactStatus = CurrentStore.Settings
                .GetSettingValue<string>(CustomerSettings.ContactDefaultStatus.Name, null);

            DefaultOrganizationStatus = CurrentStore.Settings
                .GetSettingValue<string>(CustomerSettings.OrganizationDefaultStatus.Name, null);

            MaintainerRole = await GetMaintainerRole(result, tokenSource);
        }

        protected virtual async Task ProcessRequestAsync(RegisterRequestCommand request, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
        {
            // Map incoming entities from request to Virto Commerce entities
            var account = ToApplicationUser(request.Account);

            var contact = await ToContact(request.Contact, account, request.LanguageCode);
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
            var emailVerificationFlow = CurrentStore.GetEmailVerificationFlow();

            // make user with confirmed email immediately if no email verification flow is seleced
            // other two flows allow user to confirm email
            account.EmailConfirmed = emailVerificationFlow == RegistrationFlows.NoEmailVerification;
            // lock account before confirming email
            account.LockoutEnd = emailVerificationFlow == RegistrationFlows.EmailVerificationRequired ? DateTime.MaxValue : null;

            var identityResult = await _accountService.CreateAccountAsync(account);
            result.AccountCreationResult = ToAccountCreationResult(identityResult, account, account.LockoutEnd.HasValue);

            if (!identityResult.Succeeded)
            {
                tokenSource.Cancel();
                return;
            }

            // Save account to result
            result.Contact.SecurityAccounts = new List<ApplicationUser> { account };

            // Send email notifications
            var registrationNotificationRequest = new SendRegistrationNotificationCommand
            {
                Store = CurrentStore,
                LanguageCode = request.LanguageCode,
                Organization = organization,
                Contact = contact,
            };

            switch (emailVerificationFlow)
            {
                case RegistrationFlows.NoEmailVerification:
                    {
                        await SendRegistrationEmailNotificationAsync(registrationNotificationRequest, tokenSource);
                        break;
                    }

                case RegistrationFlows.EmailVerificationOptional:
                    {
                        await SendRegistrationEmailNotificationAsync(registrationNotificationRequest, tokenSource);
                        await SendVerifyEmailCommandAsync(request, account.Email, tokenSource);
                        break;
                    }
                case RegistrationFlows.EmailVerificationRequired:
                    {
                        await SendVerifyEmailCommandAsync(request, account.Email, tokenSource);
                        break;
                    }
            }
        }

        private async Task<bool> ValidateAsync(Organization organization, Contact contact, Account account, RegisterOrganizationResult result, CancellationTokenSource tokenSource)
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

        private async Task<Organization> ToOrganization(RegisteredOrganization organization, Contact contact, ApplicationUser account)
        {
            var result = _mapper.Map<Organization>(organization);

            result.Status = DefaultOrganizationStatus;
            result.OwnerId = contact.Id;
            result.Emails = new List<string> { ResolveEmail(account, result.Addresses ?? new List<Address>()) };

            await SetDynamicPropertiesAsync(organization.DynamicProperties, result);


            return result;
        }

        private async Task<Contact> ToContact(RegisteredContact contact, ApplicationUser account, string language)
        {
            var result = _mapper.Map<Contact>(contact);

            result.Id = Guid.NewGuid().ToString();
            result.FullName = $"{contact.FirstName} {contact.LastName}";
            result.Name = result.FullName;
            result.Status = DefaultContactStatus;
            result.Emails = new List<string> { account.Email };
            result.DefaultLanguage = language;

            await SetDynamicPropertiesAsync(contact.DynamicProperties, result);

            return result;
        }

        protected virtual Task SendRegistrationEmailNotificationAsync(SendRegistrationNotificationCommand request, CancellationTokenSource tokenSource)
        {
            return _mediator.Send(request, tokenSource.Token);
        }

        protected virtual Task SendVerifyEmailCommandAsync(RegisterRequestCommand request, string email, CancellationTokenSource tokenSource)
        {
            return _mediator.Send(new SendVerifyEmailCommand(request.StoreId,
                request.LanguageCode,
                email,
                string.Empty), tokenSource.Token);
        }

        private static string ResolveEmail(ApplicationUser account, IList<Address> orgAddresses)
        {
            return orgAddresses.FirstOrDefault()?.Email ?? account.Email;
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

        protected virtual AccountCreationResult ToAccountCreationResult(IdentityResult identityResult, ApplicationUser account, bool requireEmailVerification)
        {
            return new AccountCreationResult
            {
                Succeeded = identityResult.Succeeded,
                RequireEmailVerification = requireEmailVerification,
                AccountName = account.UserName,
                Errors = identityResult.Errors.Select(x => new RegistrationError
                {
                    Code = x.Code,
                    Description = x.Description,
                    Parameter = x is CustomIdentityError error ? error.Parameter : null
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
