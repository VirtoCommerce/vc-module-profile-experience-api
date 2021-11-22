using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdatePersonalDataType : InputObjectGraphType
    {
        public InputUpdatePersonalDataType()
        {
            Field<NonNullGraphType<InputPersonalDataType>>("PersonalData");
        }
    }
}
