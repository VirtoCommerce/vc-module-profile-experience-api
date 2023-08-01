using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.StoreModule.Core.Services;
using RegistrationFlows = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants.RegistrationFlows;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ConfirmEmailCommandHandler : UserCommandHandlerBase, IRequestHandler<ConfirmEmailCommand, IdentityResultResponse>
    {
        private readonly IStoreService _storeService;
        private readonly IMemberService _memberService;
        private readonly IMediator _mediator;

        public ConfirmEmailCommandHandler(IStoreService storeService,
            IMemberService memberService,
            IMediator mediator,
            Func<UserManager<ApplicationUser>> userManager,
            IOptions<AuthorizationOptions> securityOptions)
            : base(userManager, securityOptions)
        {
            _storeService = storeService;
            _memberService = memberService;
            _mediator = mediator;
        }

        public async Task<IdentityResultResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var result = new IdentityResultResponse();
            IdentityResult identityResult;

            using var userManager = _userManagerFactory();

            var user = await userManager.FindByIdAsync(request.UserId);

            if (user is null)
            {
                identityResult = IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found" });
            }
            else if (!IsUserEditable(user.UserName))
            {
                identityResult = IdentityResult.Failed(new IdentityError { Code = "UserNotEditable", Description = "It is forbidden to edit this user." });
            }
            else
            {
                identityResult = await userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(request.Token));

                if (identityResult.Succeeded)
                {
                    if (user.LockoutEnd != null)
                    {
                        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MinValue.ToUniversalTime());
                    }

                    var store = await _storeService.GetByIdAsync(user.StoreId);
                    var emailVerificationFlow = store.GetEmailVerificationFlow();

                    if (emailVerificationFlow == RegistrationFlows.EmailVerificationRequired)
                    {
                        var contact = (await _memberService.GetByIdAsync(user.MemberId)) as Contact;
                        if (contact != null)
                        {
                            // try to find organization
                            var organization = await GetOrganization(contact);

                            var registrationNotificationRequest = new SendRegistrationNotificationCommand
                            {
                                Store = store,
                                LanguageCode = contact.DefaultLanguage ?? store.DefaultLanguage,
                                Contact = contact,
                                Organization = organization,
                            };

                            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            await _mediator.Send(registrationNotificationRequest, cancellationTokenSource.Token);
                        }
                    }
                }
            }

            result.Errors = identityResult?.Errors.Select(x => x.MapToIdentityErrorInfo()).ToList();
            result.Succeeded = identityResult?.Succeeded ?? false;

            return result;
        }

        private async Task<Organization> GetOrganization(Contact contact)
        {
            var organization = default(Organization);

            var organizationid = contact.Organizations?.FirstOrDefault();
            if (organizationid != null)
            {
                organization = await _memberService.GetByIdAsync(organizationid) as Organization;
            }

            return organization;
        }
    }
}
