using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class MakeAddressDefaultCommand : ICommand<bool>
{
    public string UserId { get; set; }
    public string MemberId { get; set; }
    public string AddressId { get; set; }
}

public class MakeAddressDefaultCommandType : InputObjectGraphType<MakeAddressDefaultCommand>
{
    public MakeAddressDefaultCommandType()
    {
        Field(x => x.AddressId);
        Field(x => x.MemberId);
    }
}
