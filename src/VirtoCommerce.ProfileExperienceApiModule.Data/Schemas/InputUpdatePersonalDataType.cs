using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdatePersonalDataType : ExtendableInputObjectGraphType
    {
        public InputUpdatePersonalDataType()
        {
            Field<NonNullGraphType<InputPersonalDataType>>("PersonalData");
        }
    }
}
