using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputRemoveMemberFromOrganizationType : ExtendableInputObjectGraphType
    {
        public InputRemoveMemberFromOrganizationType()
        {
            Field<StringGraphType>("contactId");
            Field<StringGraphType>("organizationId");
        }
    }
}
