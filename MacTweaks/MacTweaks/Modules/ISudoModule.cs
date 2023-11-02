using MacTweaks.Helpers;

namespace MacTweaks.Modules
{
    public interface ISudoModule: IModule
    {
        public static readonly bool IsSudoUser = AccessibilityHelpers.IsSudoUser;
    }
}