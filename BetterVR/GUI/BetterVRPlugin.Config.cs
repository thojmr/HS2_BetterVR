using BepInEx.Configuration;
using HarmonyLib;

namespace BetterVR 
{
    public partial class BetterVRPlugin 
    {
        public static ConfigEntry<bool> EnableControllerColliders { get; private set; }
        public static ConfigEntry<float> SetVRControllerPointerAngle { get; private set; }
        public static ConfigEntry<bool> SqueezeToTurn { get; private set; }
        public static ConfigEntry<bool> FixWorldSizeScale { get; private set; }
        public static ConfigEntry<bool> MultipleRandomHeroine { get; private set; }


        /// <summary>
        /// Init the Bepinex config manager options
        /// </summary>
        public void PluginConfigInit() 
        {

            EnableControllerColliders = Config.Bind<bool>("VR General", "Enable Controller Colliders (boop!)", true, 
                "Allows collision of VR controllers with all dynamic bones");
            EnableControllerColliders.SettingChanged += EnableControllerColliders_SettingsChanged;  

            SqueezeToTurn = Config.Bind<bool>("VR General", "Squeeze to Turn", true, 
                new ConfigDescription("Allows you to turn the headset with hand rotation while zqueezing the controller."));

            SetVRControllerPointerAngle = Config.Bind<float>("VR General", "(Not working yet)Laser Pointer Angle", 0, 
                new ConfigDescription("0 is the default angle, and negative is down.",
                new AcceptableValueRange<float>(-90, 90)));
            SetVRControllerPointerAngle.SettingChanged += SetVRControllerPointerAngle_SettingsChanged; 

            FixWorldSizeScale = Config.Bind<bool>("VR General", "Fix World Scale", true, 
                new ConfigDescription("Everything appears larger in VR, so this will shrink the worldsize down to a more realistic size."));
            FixWorldSizeScale.SettingChanged += FixWorldSizeScale_SettingsChanged; 

            MultipleRandomHeroine = Config.Bind<bool>("VR General", "Multiple Heroine when Random", false, 
                new ConfigDescription("Will add 2 Heroine to the HScene when the 'Random' button is selected. (Default is 1)"));           

        }      


        /// <summary>
        /// On config options changed by user, trigger stuff
        /// </summary>
        internal void EnableControllerColliders_SettingsChanged(object sender, System.EventArgs e) 
        {
            if (!EnableControllerColliders.Value) 
            {            
                VRControllerColliderHelper.StopHelperCoroutine();                                      
            } 
            else 
            {                
                VRControllerColliderHelper.TriggerHelperCoroutine();
            }
        }


        internal void SetVRControllerPointerAngle_SettingsChanged(object sender, System.EventArgs e) 
        {
            VRControllerPointer.UpdateOneOrMoreCtrlPointers(SetVRControllerPointerAngle.Value);
        }


        internal void FixWorldSizeScale_SettingsChanged(object sender, System.EventArgs e) 
        {
            BetterVRPluginHelper.FixWorldScale(FixWorldSizeScale.Value);
        }


    }
}