using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, IdentityResult>
    {
        private readonly IAccountService _accountService;
        private readonly IMediator _mediator;

        public CreateUserCommandHandler(IAccountService accountService, IMediator mediator)
        {
            _accountService = accountService;
            _mediator = mediator;
        }

        public virtual async Task<IdentityResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            request.ApplicationUser = GetUserWithProfileFieldsOnly(request.ApplicationUser);

            var result = await _accountService.CreateAccountAsync(request.ApplicationUser);

            if (result.Succeeded)
            {
                // Send Email Verification
                await _mediator.Send(new SendVerifyEmailCommand(request.ApplicationUser.StoreId, string.Empty, request.ApplicationUser.Email, request.ApplicationUser.Id), cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Builds a new customer from the profile fields of the requested user.
        /// Everything else (id, roles, logins, member, user type, confirmation, lockout and 2FA flags) gets its default value.
        /// </summary>
        protected virtual ApplicationUser GetUserWithProfileFieldsOnly(ApplicationUser requestedUser)
        {
            var result = AbstractTypeFactory<ApplicationUser>.TryCreateInstance();

            // A caller-chosen id could match an existing contact id, so always generate a new one
            result.Id = Guid.NewGuid().ToString();
            result.UserName = requestedUser.UserName;
            result.Email = requestedUser.Email;
            result.Password = requestedUser.Password;
            result.PhoneNumber = requestedUser.PhoneNumber;
            result.PhotoUrl = requestedUser.PhotoUrl;
            result.StoreId = requestedUser.StoreId;
            result.UserType = nameof(UserType.Customer);

            return result;
        }
    }
}
