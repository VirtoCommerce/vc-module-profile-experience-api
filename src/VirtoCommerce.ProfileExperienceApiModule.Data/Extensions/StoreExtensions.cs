using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using StoreSettings = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions
{
    public static class StoreExtensions
    {
        public static string GetEmailVerificationFlow(this Store stroe)
        {
            var emailVerificationEnabled = stroe.Settings.GetSettingValue(StoreSettings.EmailVerificationEnabled.Name, (bool)StoreSettings.EmailVerificationEnabled.DefaultValue);
            var emailVerificationRequired = stroe.Settings.GetSettingValue(StoreSettings.EmailVerificationRequired.Name, (bool)StoreSettings.EmailVerificationRequired.DefaultValue);

            if (!emailVerificationEnabled)
            {
                return ModuleConstants.RegistrationFlows.NoEmailVerification;
            }
            else if (emailVerificationRequired)
            {
                return ModuleConstants.RegistrationFlows.EmailVerificationRequired;
            }
            else
            {
                return ModuleConstants.RegistrationFlows.EmailVerificationOptional;
            }
        }
    }
}
