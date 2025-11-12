using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class SendPasswordResetEmailCommandBuilder : CommandBuilder<SendPasswordResetEmailCommand, bool, SendPasswordResetEmailCommandType, BooleanGraphType>
{
    protected override string Name => "sendPasswordResetEmail";

    public SendPasswordResetEmailCommandBuilder(IMediator mediator, IAuthorizationService authorizationService) : base(mediator, authorizationService)
    {
    }
}
