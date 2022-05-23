using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Models;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Notifications;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterCompanyCommandHandler : IRequestHandler<RegisterCompanyCommand, RegisterCompanyAggregate>
    {
        private readonly IMapper _mapper;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        private readonly IMemberService _memberService;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly IStoreNotificationSender _storeNotificationSender;
        private readonly RoleManager<Role> _roleManager;
        private readonly ICrudService<Store> _storeService;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        private const string Creator = "frontend";
        private const string UserType = "Manager";
        private const string MaintainerRoleId = "org-maintainer";

        public RegisterCompanyCommandHandler(IMapper mapper,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            IMemberService memberService,
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IStoreNotificationSender storeNotificationSender,
            RoleManager<Role> roleManager,
            ICrudService<Store> storeService,
            INotificationSearchService notificationSearchService,
            INotificationSender notificationSender)
        {
            _mapper = mapper;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _memberService = memberService;
            _userManagerFactory = userManagerFactory;
            _storeNotificationSender = storeNotificationSender;
            _roleManager = roleManager;
            _storeService = storeService;
            _notificationSearchService = notificationSearchService;
            _notificationSender = notificationSender;
        }

        public virtual async Task<RegisterCompanyAggregate> Handle(RegisterCompanyCommand request,
            CancellationToken cancellationToken)
        {
            var aggregate = new RegisterCompanyAggregate();

            try
            {
                var company = _mapper.Map<Organization>(request.Company);
                var owner = _mapper.Map<Contact>(request.Owner);
                var account = request.Account;
                
                await SetDynamicPropertiesAsync(request.Owner.DynamicProperties, owner);

                var store = await _storeService.GetByIdAsync(request.StoreId);

                if (store == null)
                {
                    throw new ArgumentException($"Store {request.StoreId} not found");
                }
                
                var contactStatus = store.Settings
                    .GetSettingValue<string>(ModuleConstants.Settings.General.ContactDefaultStatus.Name, null);

                if (company != null)
                {
                    await SetDynamicPropertiesAsync(request.Company.DynamicProperties, company);
                    var organizationStatus = store
                        .Settings
                        .GetSettingValue<string>(ModuleConstants.Settings.General.OrganizationDefaultStatus.Name, null);
                    company.CreatedBy = Creator;
                    company.Status = organizationStatus;

                    await _memberService.SaveChangesAsync(new Member[] { company });
                    aggregate.Company = company;
                }
                
                owner.Status = contactStatus;
                owner.Organizations = company != null ? new List<string> { company.Id } : null;
                await _memberService.SaveChangesAsync(new Member[] { owner });
                aggregate.Owner = owner;

                var maintainerRole = await _roleManager.FindByIdAsync(MaintainerRoleId);
                if (maintainerRole == null)
                {
                    throw new ArgumentException($"Organization maintainer role with id {MaintainerRoleId} not found");
                }

                account.StoreId = request.StoreId;
                account.Status = contactStatus;
                account.UserType = UserType;
                account.MemberId = owner.Id;
                account.Roles = new List<Role> { maintainerRole };
                aggregate.AccountCreationResult = await CreateAccountAsync(account);

                if (!aggregate.AccountCreationResult.Succeeded)
                {
                    await RollBackMembersCreationAsync(aggregate);
                    return aggregate;
                }

                aggregate.Account = account;

                if (company != null)
                {
                    await SendNotificationAsync(account.Email, store.Email, company.Name);
                }

                return aggregate;
            }
            catch (Exception)
            {
                await RollBackMembersCreationAsync(aggregate);
                throw;
            }
        }

        private async Task RollBackMembersCreationAsync(RegisterCompanyAggregate aggregate)
        {
            var ids = new[] { aggregate.Company?.Id, aggregate.Owner?.Id }
                .Where(x => x != null)
                .ToArray();
            await _memberService.DeleteAsync(ids);

            aggregate.Company = null;
            aggregate.Owner = null;
        }

        private async Task<IdentityResult> CreateAccountAsync(ApplicationUser account)
        {
            var result = default(IdentityResult);

            using var userManager = _userManagerFactory();
            if (account.Password.IsNullOrEmpty())
            {
                result = await userManager.CreateAsync(account);
            }
            else
            {
                result = await userManager.CreateAsync(account, account.Password);
            }

            if (result.Succeeded)
            {
                var user = await userManager.FindByNameAsync(account.UserName);

                await _storeNotificationSender.SendUserEmailVerificationAsync(user);
            }

            return result;
        }

        private async Task SetDynamicPropertiesAsync(IList<DynamicPropertyValue> dynamicProperties, IHasDynamicProperties entity)
        {
            if (dynamicProperties?.Any() ?? false)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(entity, dynamicProperties);
            }
        }

        protected virtual async Task SendNotificationAsync(string recipientEmail, string senderEmail, string companyName)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<RegisterCompanyEmailNotification>();
            notification.To = recipientEmail;
            notification.From = senderEmail;
            notification.CompanyName = companyName;

            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }
    }
}
