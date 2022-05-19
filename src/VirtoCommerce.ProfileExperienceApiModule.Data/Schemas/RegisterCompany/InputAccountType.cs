using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputAccountType : InputObjectGraphType
    {
        public InputAccountType()
        {
            Field<NonNullGraphType<StringGraphType>>("username");
            Field<NonNullGraphType<StringGraphType>>("email");
            Field<NonNullGraphType<StringGraphType>>("password");
        }
    }
}
