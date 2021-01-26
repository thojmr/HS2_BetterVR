using ActionGame;
using KKAPI.MainGame;

namespace BetterVR 
{
    public class VRCameraGameController : GameCustomFunctionController
    {
        protected override void OnDayChange(Cycle.Week day)
        {
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" OnDayChange {day}");
            VRCameraController.ClearLastPosition();
        }

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" OnStartH ");
            VRCameraController.ClearLastPosition();
        }

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" OnEndH ");
            VRCameraController.ClearLastPosition();
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            if (BetterVRPlugin.debugLog) BetterVRPlugin.Logger.LogInfo($" OnPeriodChange ");
            VRCameraController.ClearLastPosition();
        }
    }
}