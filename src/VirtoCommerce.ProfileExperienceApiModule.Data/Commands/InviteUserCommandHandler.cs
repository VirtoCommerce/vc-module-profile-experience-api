using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, IdentityResultResponse>
    {
        private readonly IInviteCustomerService _inviteCustomerService;

        public InviteUserCommandHandler(IInviteCustomerService inviteCustomerService)
        {
            _inviteCustomerService = inviteCustomerService;
        }

        public virtual async Task<IdentityResultResponse> Handle(InviteUserCommand request, CancellationToken cancellationToken)
        {
            var inviteRequest = ToInviteCustomerRequest(request);

            var inviteResult = await _inviteCustomerService.InviteCustomerAsyc(inviteRequest, cancellationToken);

            return OrganizationInviteHelper.ToIdentityResultResponse(inviteResult);
        }

        protected virtual InviteCustomerRequest ToInviteCustomerRequest(InviteUserCommand request)
        {
            var additionalParameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(request.CustomerOrderId))
            {
                additionalParameters["customerOrderId"] = request.CustomerOrderId;
            }

            return new InviteCustomerRequest
            {
                StoreId = request.StoreId,
                OrganizationId = request.OrganizationId,
                UrlSuffix = request.UrlSuffix,
                Emails = request.Emails,
                Message = request.Message,
                RoleIds = request.RoleIds,
                AdditionalParameters = additionalParameters,
            };
        }
    }
}
