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

        public CreateUserCommandHandler(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public virtual Task<IdentityResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            return _accountService.CreateAccountAsync(request.ApplicationUser);
        }
    }
}
