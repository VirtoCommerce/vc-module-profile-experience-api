using System;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class SendPasswordResetEmailCommandBuilder : CommandBuilder<SendPasswordResetEmailCommand, bool, SendPasswordResetEmailCommandType, BooleanGraphType>
{
    protected override string Name => "sendPasswordResetEmail";

    public SendPasswordResetEmailCommandBuilder(IAuthorizationService authorizationService) : base(authorizationService)
    {
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public SendPasswordResetEmailCommandBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : this(authorizationService)
    {
    }
}
