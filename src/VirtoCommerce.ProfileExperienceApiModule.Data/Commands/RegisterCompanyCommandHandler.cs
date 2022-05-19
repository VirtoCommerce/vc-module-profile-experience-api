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

        private const string Creator = "frontend";

        public RegisterCompanyCommandHandler(IMapper mapper,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            IMemberService memberService,
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IStoreNotificationSender storeNotificationSender)
        {
            _mapper = mapper;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _memberService = memberService;
            _userManagerFactory = userManagerFactory;
            _storeNotificationSender = storeNotificationSender;
        }

        public virtual async Task<RegisterCompanyAggregate> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
        {
            var aggregate = new RegisterCompanyAggregate();

            var company = _mapper.Map<Organization>(request.Company);
            var owner = _mapper.Map<Contact>(request.Owner);
            var account = request.Account;

            await SetDynamicProperties(request.Company.DynamicProperties, company);
            await SetDynamicProperties(request.Owner.DynamicProperties, owner);

            company.CreatedBy = Creator;
            company.Status = "New"; //take from settings
            owner.Status = "New";
            account.StoreId = request.StoreId;
            account.Status = "New";
            account.UserType = "Manager";

            await _memberService.SaveChangesAsync(new Member[] { company });

            owner.Organizations = new List<string> { company.Id };
            
            await _memberService.SaveChangesAsync(new Member[] { owner });

            account.MemberId =owner.Id;

            using (var userManager = _userManagerFactory())
            {
                var result = default(IdentityResult);

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

                    aggregate.Account = user;
                }

                aggregate.AccountCreationResult = result;
            }

            aggregate.Company = company;
            aggregate.Owner = owner;
            
            return aggregate;
        }

        private async Task SetDynamicProperties(IList<DynamicPropertyValue> dynamicProperties,
            IHasDynamicProperties entity)
        {
            if (dynamicProperties?.Any() ?? false)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(entity, dynamicProperties);
            }
        }
    }
}
