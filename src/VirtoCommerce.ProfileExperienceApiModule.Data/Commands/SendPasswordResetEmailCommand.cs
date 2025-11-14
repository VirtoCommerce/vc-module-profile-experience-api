using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class SendPasswordResetEmailCommand : ICommand<bool>
{
    public string StoreId { get; set; }

    public string CultureName { get; set; }

    public string LoginOrEmail { get; set; }

    public string UrlSuffix { get; set; }
}

public class SendPasswordResetEmailCommandType : ExtendableInputObjectGraphType<SendPasswordResetEmailCommand>
{
    public SendPasswordResetEmailCommandType()
    {
        Field(x => x.StoreId, nullable: true);
        Field(x => x.CultureName, nullable: true);
        Field(x => x.LoginOrEmail);
        Field(x => x.UrlSuffix, nullable: true);
    }
}
