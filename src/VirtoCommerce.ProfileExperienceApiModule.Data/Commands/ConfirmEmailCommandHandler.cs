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
                    var unlockContactCommand = new UnlockOrganizationContactCommand { UserId = user.MemberId };
                    await _mediator.Send(unlockContactCommand, cancellationToken);

                    await SendRegistrationNotification(user, cancellationToken);
                }
            }

            result.Errors = identityResult?.Errors.Select(x => x.MapToIdentityErrorInfo()).ToList();
            result.Succeeded = identityResult?.Succeeded ?? false;

            return result;
        }

        protected virtual async Task SendRegistrationNotification(ApplicationUser user, CancellationToken cancellationToken)
        {
            var store = await _storeService.GetByIdAsync(user.StoreId);
            if (store == null)
            {
                return;
            }

            var emailVerificationFlow = store.GetEmailVerificationFlow();
            if (emailVerificationFlow != ModuleConstants.RegistrationFlows.EmailVerificationRequired)
            {
                return;
            }

            var contact = await _memberService.GetByIdAsync(user.MemberId) as Contact;
            if (contact == null)
            {
                return;
            }

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

        protected virtual async Task<Organization> GetOrganization(Contact contact)
        {
            var organization = default(Organization);

            var organizationId = contact.Organizations?.FirstOrDefault();
            if (organizationId != null)
            {
                organization = await _memberService.GetByIdAsync(organizationId) as Organization;
            }

            return organization;
        }
    }
}
