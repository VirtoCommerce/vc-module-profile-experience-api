using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputRemoveMemberFromOrganizationType : InputObjectGraphType
    {
        public InputRemoveMemberFromOrganizationType()
        {
            Field<StringGraphType>("contactId");
            Field<StringGraphType>("organizationId");
        }
    }
}
