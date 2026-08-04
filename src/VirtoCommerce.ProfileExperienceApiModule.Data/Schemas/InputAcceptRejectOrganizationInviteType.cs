using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputAcceptRejectOrganizationInviteType : ExtendableInputObjectGraphType
    {
        public InputAcceptRejectOrganizationInviteType()
        {
            Field<NonNullGraphType<StringGraphType>>("OrganizationId").Description("ID of the organization the current user was invited to");
        }
    }
}
