using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtoCommerce.ProfileExperienceApiModule.Data
{
    public static class ModuleConstants
    {
        public static class Security
        {
            public static class Permissions
            {
                public const string MyOrganizationEdit = "xapi:my_organization:edit";

                public static string[] AllPermissions { get; } = { MyOrganizationEdit };
            }
        }

    }
}
