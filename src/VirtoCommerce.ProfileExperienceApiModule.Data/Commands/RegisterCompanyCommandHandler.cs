using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Models;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.StoreModule.Core.Services;

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

        private const string Creator = "frontend";
        private const string MaintainerRoleId = "org-maintainer";

        public RegisterCompanyCommandHandler(IMapper mapper,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            IMemberService memberService,
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IStoreNotificationSender storeNotificationSender,
            RoleManager<Role> roleManager)
        {
            _mapper = mapper;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _memberService = memberService;
            _userManagerFactory = userManagerFactory;
            _storeNotificationSender = storeNotificationSender;
            _roleManager = roleManager;
        }

        public virtual async Task<RegisterCompanyAggregate> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
        {
            var aggregate = new RegisterCompanyAggregate();

            try
            {
                var company = _mapper.Map<Organization>(request.Company);
                var owner = _mapper.Map<Contact>(request.Owner);
                var account = request.Account;

                await SetDynamicPropertiesAsync(request.Company.DynamicProperties, company);
                await SetDynamicPropertiesAsync(request.Owner.DynamicProperties, owner);

                company.CreatedBy = Creator;
                company.Status = "New"; //take from settings
                owner.Status = "New";
                account.StoreId = request.StoreId;
                account.Status = "New";
                account.UserType = "Manager";

                await _memberService.SaveChangesAsync(new Member[] { company });
                aggregate.Company = company;

                owner.Organizations = new List<string> { company.Id };
                await _memberService.SaveChangesAsync(new Member[] { owner });
                aggregate.Owner = owner;

                var maintainerRole = await _roleManager.FindByIdAsync(MaintainerRoleId);

                if (maintainerRole == null)
                {
                    throw new Exception($"Organization maintainer role with id {MaintainerRoleId} not found");
                }

                account.MemberId = owner.Id;
                account.Roles = new List<Role> { maintainerRole };
                aggregate.AccountCreationResult = await CreateAccountAsync(account);

                if (!aggregate.AccountCreationResult.Succeeded)
                {
                    await RollBackMembersCreationAsync(aggregate);
                    return aggregate;
                }

                aggregate.Account = account;
            
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
    }
}
