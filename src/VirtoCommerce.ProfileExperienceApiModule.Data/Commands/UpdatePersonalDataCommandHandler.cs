using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdatePersonalDataCommandHandler : UserCommandHandlerBase, IRequestHandler<UpdatePersonalDataCommand, IdentityResult>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;

        public UpdatePersonalDataCommandHandler(
            IContactAggregateRepository contactAggregateRepository
            , Func<UserManager<ApplicationUser>> userManager
            , IOptions<AuthorizationOptions> securityOptions
            )
            : base(userManager, securityOptions)
        {
            _contactAggregateRepository = contactAggregateRepository;
        }

        public virtual async Task<IdentityResult> Handle(UpdatePersonalDataCommand request, CancellationToken cancellationToken)
        {
            var result = IdentityResult.Success;
            using (var userManager = _userManagerFactory())
            {
                var user = await userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return IdentityResult.Failed();
                }
                if (!IsUserEditable(user.UserName))
                {
                    return IdentityResult.Failed(new IdentityError { Description = "It is forbidden to edit this user." });
                }
                if (request.PersonalData?.Email != null && user.Email != request.PersonalData?.Email)
                {
                    user.Email = request.PersonalData.Email;
                    result = await userManager.UpdateAsync(user);
                }

                var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(user.MemberId);
                if (contactAggregate != null)
                {
                    contactAggregate.UpdatePersonalDetails(request.PersonalData);

                    await _contactAggregateRepository.SaveAsync(contactAggregate);
                }
            }
            return result;

        }
    }
}
