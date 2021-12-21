using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputSendVerifyEmailType : InputObjectGraphType
    {
        public InputSendVerifyEmailType()
        {
            Field<StringGraphType>("email");
        }
    }
}
