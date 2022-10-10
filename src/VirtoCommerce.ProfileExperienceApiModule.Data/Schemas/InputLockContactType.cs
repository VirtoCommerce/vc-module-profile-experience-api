using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputLockUnlockOrganizationContactType : InputObjectGraphType
    {
        public InputLockUnlockOrganizationContactType()
        {
            Field<StringGraphType>("UserId");
        }
    }
}
