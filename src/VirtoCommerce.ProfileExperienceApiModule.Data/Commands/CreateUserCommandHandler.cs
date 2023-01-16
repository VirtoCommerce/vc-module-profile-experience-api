using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
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
            var result = await _accountService.CreateAccountAsync(request.ApplicationUser);

            if (result.Succeeded)
            {
                // Send Email Verification
                await _mediator.Send(new SendVerifyEmailCommand(request.ApplicationUser.StoreId, string.Empty, request.ApplicationUser.Email));

            }

            return result;
        }
    }
}
