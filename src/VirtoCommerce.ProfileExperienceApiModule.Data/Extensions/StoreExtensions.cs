using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using StoreSettings = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions
{
    public static class StoreExtensions
    {
        public static string GetEmailVerificationFlow(this Store store)
        {
            var emailVerificationEnabled = store.Settings.GetSettingValue(StoreSettings.EmailVerificationEnabled.Name, (bool)StoreSettings.EmailVerificationEnabled.DefaultValue);
            var emailVerificationRequired = store.Settings.GetSettingValue(StoreSettings.EmailVerificationRequired.Name, (bool)StoreSettings.EmailVerificationRequired.DefaultValue);

            if (!emailVerificationEnabled)
            {
                return ModuleConstants.RegistrationFlows.NoEmailVerification;
            }

            if (emailVerificationRequired)
            {
                return ModuleConstants.RegistrationFlows.EmailVerificationRequired;
            }

            return ModuleConstants.RegistrationFlows.EmailVerificationOptional;
        }
    }
}
