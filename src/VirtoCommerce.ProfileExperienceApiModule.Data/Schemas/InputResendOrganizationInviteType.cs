using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputResendOrganizationInviteType : ExtendableInputObjectGraphType
    {
        public InputResendOrganizationInviteType()
        {
            Field<NonNullGraphType<StringGraphType>>("MemberId").Description("Contact member ID");
            Field<NonNullGraphType<StringGraphType>>(nameof(ResendOrganizationInviteCommand.UrlSuffix)).Description("Optional URL suffix: relative URL to the page which handles registration by invite");
            Field<StringGraphType>(nameof(ResendOrganizationInviteCommand.Message)).Description("Optional message to include into the invitation email");
        }
    }
}
