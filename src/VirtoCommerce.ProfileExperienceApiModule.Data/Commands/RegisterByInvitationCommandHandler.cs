using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.XOrder.Commands;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterByInvitationCommandHandler : IRequestHandler<RegisterByInvitationCommand, IdentityResultResponse>
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IMemberService _memberService;
        private readonly IStoreService _storeService;
        private readonly IMediator _mediator;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

        public RegisterByInvitationCommandHandler(
            IWebHostEnvironment environment,
            Func<UserManager<ApplicationUser>> userManager,
            IMemberService memberService,
            IStoreService storeService,
            IMediator mediator)
        {
            _environment = environment;
            _userManagerFactory = userManager;
            _memberService = memberService;
            _storeService = storeService;
            _mediator = mediator;
        }

        public virtual async Task<IdentityResultResponse> Handle(RegisterByInvitationCommand request, CancellationToken cancellationToken)
        {
            using var userManager = _userManagerFactory();

            var user = await userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                var errors = _environment.IsDevelopment() ? new[] { new IdentityError { Code = "UserNotFound", Description = "User not found" } } : null;
                return SetResponse(IdentityResult.Failed(errors));
            }

            var identityResult = await userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(request.Token), request.Password);
            if (!identityResult.Succeeded)
            {
                return SetResponse(identityResult);
            }

            identityResult = await userManager.SetUserNameAsync(user, request.Username);
            if (!identityResult.Succeeded)
            {
                return SetResponse(identityResult);
            }

            user.EmailConfirmed = true;
            identityResult = await userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                return SetResponse(identityResult);
            }

            var contact = await _memberService.GetByIdAsync(user.MemberId) as Contact;
            if (contact == null)
            {
                var errors = _environment.IsDevelopment() ? new[] { new IdentityError { Code = "ContactNotFound", Description = "Contact not found" } } : null;
                identityResult = IdentityResult.Failed(errors);
            }
            else
            {
                contact.FirstName = request.FirstName;
                contact.LastName = request.LastName;
                contact.FullName = $"{request.FirstName} {request.LastName}";
                if (!string.IsNullOrEmpty(request.Phone))
                {
                    contact.Phones = new List<string> { request.Phone };
                }

                await _memberService.SaveChangesAsync(new Member[] { contact });

                // associate order
                if (!string.IsNullOrEmpty(request.CustomerOrderId))
                {
                    await TransferOrderAsync(request.CustomerOrderId, user.Id, contact.FullName, cancellationToken);
                }

                await SendRegistrationNotificationAsync(user, contact, cancellationToken);
            }

            return SetResponse(identityResult);
        }

        private static IdentityResultResponse SetResponse(IdentityResult identityResult) => new()
        {
            Errors = identityResult.Errors.Select(x => x.MapToIdentityErrorInfo()).ToList(),
            Succeeded = identityResult.Succeeded,
        };

        private async Task SendRegistrationNotificationAsync(ApplicationUser user, Contact contact, CancellationToken cancellationToken)
        {
            var store = await _storeService.GetByIdAsync(user.StoreId);
            if (store == null)
            {
                return;
            }

            var registrationNotificationRequest = new SendRegistrationNotificationCommand
            {
                Store = store,
                LanguageCode = contact.DefaultLanguage ?? store.DefaultLanguage,
                Contact = contact,
            };

            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _mediator.Send(registrationNotificationRequest, cancellationTokenSource.Token);
        }

        private async Task TransferOrderAsync(string customerOrderId, string userId, string userName, CancellationToken cancellationToken)
        {
            var transferOrderCommand = new TransferOrderCommand
            {
                CustomerOrderId = customerOrderId,
                ToUserId = userId,
                UserName = userName,
            };

            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _mediator.Send(transferOrderCommand, cancellationTokenSource.Token);
        }
    }
}
