using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace BetterVR 
{
    public partial class BetterVRPlugin
    {
        public static ConfigEntry<bool> EnableControllerColliders { get; private set; }
        public static ConfigEntry<float> ControllerColliderRadius { get; private set; }
        public static ConfigEntry<string> GestureStrip { get; private set; }

        public static ConfigEntry<string> HandHSpeedGesture { get; private set; }
        public static ConfigEntry<float> HandHSpeedSensitivity { get; private set; }

        public static ConfigEntry<float> HapticFeedbackIntensity { get; private set; }
        public static ConfigEntry<float> SetVRControllerPointerAngle { get; private set; }
        public static ConfigEntry<float> PlayerLogScale { get; private set; }
        public static ConfigEntry<string> SqueezeToTurn { get; private set; }
        public static ConfigEntry<bool> AllowVerticalRotation { get; private set; }
        public static ConfigEntry<bool> ToyMovesVerticallyWhenAttachedToBody { get; private set; }
        public static ConfigEntry<bool> FixWorldSizeScale { get; private set; }
        public static ConfigEntry<bool> MultipleRandomHeroine { get; private set; }
        public static ConfigEntry<bool> UsePrivacyScreen { get; private set; }
        public static ConfigEntry<bool> SkipTitleScene { get; private set; }
        public static ConfigEntry<bool> UnlockAllPositions { get; private set; }
        public static ConfigEntry<string> HandDisplay { get; private set; }
        public static ConfigEntry<Vector3> LeftGloveOffset { get; private set; }
        public static ConfigEntry<Quaternion> LeftGloveRotation { get; private set; }
        public static ConfigEntry<Vector3> RightGloveOffset { get; private set; }
        public static ConfigEntry<Quaternion> RightGloveRotation { get; private set; }
        public static ConfigEntry<float> GloveScale { get; private set; }

        public static float PlayerScale {
            get { return Mathf.Pow(2, PlayerLogScale.Value); }
            set { PlayerLogScale.Value = Mathf.Log(value, 2); }
        }

        /// <summary>
        /// Init the Bepinex config manager options
        /// </summary>
        public void PluginConfigInit() 
        {

            EnableControllerColliders = Config.Bind<bool>("VR General", "Enable Controller Colliders (boop!)", true, 
                "Allows collision of VR controllers with all dynamic bones");

            ControllerColliderRadius = Config.Bind<float>(
                "VR General", "Controller Collider Radius", 0.09f,
                 new ConfigDescription(
                     "Radius of the colliders on the controller",
                     new AcceptableValueRange<float>(0.01f, 0.5f)));

            GestureStrip = Config.Bind<string>(
                "VR General", "Enable Gesture Strip", "Right hand",
                new ConfigDescription(
                    "Enable holding trigger and dragging away to undress or holding trigger and dragging onto to dress",
                    new AcceptableValueList<string>(new string[] { "Disabled", "Left hand", "Right hand" })));

            HandHSpeedGesture = Config.Bind<string>(
                "VR General", "Hand H Speed Gesture", "Auto",
                new ConfigDescription(
                    "Enable controlling H action speed using hand motion",
                    new AcceptableValueList<string>(new string[] { "Disabled", "Button-initiated", "Auto" })));

            HandHSpeedSensitivity = Config.Bind<float>(
                "VR General", "Hand H Speed Sensitivty", 3,
                new ConfigDescription(
                    "Speed sensitivy when using hand movement to control H speed when touching certain parts",
                    new AcceptableValueRange<float>(0f, 8f)));

            HapticFeedbackIntensity = Config.Bind<float>(
                "VR General", "Haptic Feedback Intensity", 0.125f,
                new ConfigDescription("Haptic feedback intensity on controllers", new AcceptableValueRange<float>(0f, 1f)));

            SqueezeToTurn = Config.Bind<string>(
                "VR General", "Squeeze to Turn", "One-handed",
                new ConfigDescription(
                    "Allows you to turn the headset with hand rotation while squeezing the controller.",
                    new AcceptableValueList<string>(new string[] { "Disabled", "One-handed", "Two-handed"})));

            AllowVerticalRotation = Config.Bind<bool>(
                "VR General", "Allow Vertical Rotation", false, new ConfigDescription("Allows rotating the world vertically."));

            ToyMovesVerticallyWhenAttachedToBody = Config.Bind<bool>(
                "VR General", "Toy Moves Vertically When Attached To Body", true,
                new ConfigDescription("Unlocks toy vertical movement when it is attached to body."));

            PlayerLogScale = Config.Bind<float>(
                "VR General", "Log2 of Player Scale", Mathf.Log(1.15f, 2f), 
                 new ConfigDescription(
                     "Log2 of player scale when fixing world size scale, default is Log2(1.15); hold both triggers and both grips to adjust.",
                     new AcceptableValueRange<float>(-4, 4)));
            PlayerLogScale.SettingChanged += FixWorldSizeScale_SettingsChanged;

            // SetVRControllerPointerAngle = Config.Bind<float>("VR General", "(Not working yet)Laser Pointer Angle", 0, 
            //     new ConfigDescription("0 is the default angle, and negative is down.",
            //     new AcceptableValueRange<float>(-90, 90)));
            // SetVRControllerPointerAngle.SettingChanged += SetVRControllerPointerAngle_SettingsChanged; 

            FixWorldSizeScale = Config.Bind<bool>("VR General", "Fix World Scale", true, 
                new ConfigDescription("Everything appears larger in VR, so this will shrink the worldsize down to a more realistic size."));
            FixWorldSizeScale.SettingChanged += FixWorldSizeScale_SettingsChanged; 

            MultipleRandomHeroine = Config.Bind<bool>("VR General", "Multiple Heroine when Random", false, 
                new ConfigDescription("Will add 2 Heroine to the HScene when the 'Random' button is selected. (Default is 1)"));

            UsePrivacyScreen = Config.Bind<bool>("VR General", "Use Privacy Screen", true,
                new ConfigDescription("Puts a black screen on desktop window"));
            UsePrivacyScreen.SettingChanged += UsePrivacyScreen_SettingsChanged;

            SkipTitleScene = Config.Bind<bool>(
                "VR General", "Skip Title Scene", false,
                new ConfigDescription("Skip title scene and go straight to the select scene on game start."));

            UnlockAllPositions = Config.Bind<bool>(
                "VR General", "Unlock all positions", true, new ConfigDescription("Unlock all positions regardless of character status"));

            HandDisplay = Config.Bind<string>(
                "VR General", "Hand Display", "Gloves",
                new ConfigDescription(
                    "How the hands should be diplayed as in VR",
                    new AcceptableValueList<string>(new string[] { "Gloves", "Controllers", "GlovesAndControllers" })));

            LeftGloveOffset = Config.Bind(
                "VR General",
                "Left Hand Offset",
                new Vector3(-0.05f, 0.25f, -0.28f),
                "Offset of left glove relative to controller center, use radial menu option on the other hand to start adjusting and press trigger to stop adjusting");

            LeftGloveRotation = Config.Bind(
                "VR General",
                "Left Hand Rotation",
                Quaternion.Euler(315, 0, 90),
                "Rotation of left glove relative to controller, use radial menu option on the other hand to start adjusting and press trigger to stop adjusting");

            RightGloveOffset = Config.Bind(
                "VR General",
                "Right Hand Offset",
                new Vector3(0.05f, 0.25f, -0.28f),
                "Offset of right glove relative to controller center, use radial menu option on the other hand to start adjusting and press trigger to stop adjusting"); ;

            RightGloveRotation = Config.Bind(
                "VR General",
                "Right Hand Rotation",
                Quaternion.Euler(315, 0, -90),
                "Rotation of right glove relative to controller, use radial menu option on the other hand to start adjusting and press trigger to stop adjusting");

            GloveScale = Config.Bind<float>(
                "VR General", "Hand Scale", 0.14f,
                 new ConfigDescription("Scale of the VR gloves",  new AcceptableValueRange<float>(0.01f, 2f)));

        }

        internal void SetVRControllerPointerAngle_SettingsChanged(object sender, System.EventArgs e) 
        {
             VRControllerPointer.UpdateOneOrMoreCtrlPointers(SetVRControllerPointerAngle.Value);
        }

        internal static bool IsTwoHandedTurnEnabled()
        {
            return SqueezeToTurn.Value == "Two-handed";
        }

        internal static bool IsOneHandedTurnEnabled()
        {
            return SqueezeToTurn.Value == "One-handed";
        }

        internal static bool IsHandHSpeedGestureEnabled()
        {
            return HandHSpeedGesture.Value != "Disabled";
        }

        internal static bool HandHSpeedGestureRequiresButtonPress()
        {
            return HandHSpeedGesture.Value == "Button-initiated";
        }

        internal void FixWorldSizeScale_SettingsChanged(object sender, System.EventArgs e) 
        {
            BetterVRPluginHelper.FixWorldScale(FixWorldSizeScale.Value);
        }

        internal void UsePrivacyScreen_SettingsChanged(object sender, System.EventArgs e)
        {
            BetterVRPluginHelper.UpdatePrivacyScreen();
        }
    }
}
