using Manager;
using GameLoadCharaFileSystem;

namespace BetterVR
{
    public static class VRMenuRandom  
    {

        /// <summary>
        /// When the Random button is presses, set a random female/male, and start the HScene
        /// </summary>
        public static void OnSelectRandomBtn()
        {
            //Just need this for fade
            VRSelectManager vrMgr = Singleton<VRSelectManager>.Instance;

            //Get a random female and set it to the HSCeneManager
            var female = GetRandomFemale();
            Singleton<HSceneManager>.Instance.pngFemales = new string[] {female.fileName, ""};

            //Set one female and one empty
            Singleton<HSceneManager>.Instance.vrStatusInfos[0].Set(female.status, female.resistH, female.resistPain, female.resistAnal);
            Singleton<HSceneManager>.Instance.vrStatusInfos[1].Set(0, false, false, false);
            
            Singleton<HSceneManager>.Instance.mapID = Singleton<Game>.Instance.mapNo;

            //Set one male, and one empty
            Singleton<HSceneManager>.Instance.pngMale = GetRandomMale();
            Singleton<HSceneManager>.Instance.pngMaleSecond = "";

            Singleton<HSceneManager>.Instance.bFutanari = false;            
            Singleton<HSceneManager>.Instance.bFutanariSecond = false;

            //Load the HScene
            Scene.LoadReserve(new Scene.Data
            {
                levelName = "VRHScene",
                fadeType = FadeCanvas.Fade.In
            }, true);
            
            vrMgr.Fade.StartFade(FadeSphere.Fade.In, false);
            Singleton<Game>.Instance.IsFade = true;
        }


        public static int GetRandomNum(int start, int limit = 10)
        {
            return UnityEngine.Random.Range(start, limit);
        }


        public static VRSelectManager.SelectCardInfo GetRandomFemale()
        {
            //Get all female character card info list
            var females = GameCharaFileInfoAssist.CreateCharaFileInfoList(0, false, true, false, false, false, true, true);
            var femaleCount = females.Count;
            var winnerNum = GetRandomNum(0, femaleCount-1);
            var winner = females[winnerNum];

            //Shape it to match HScene expected format
            var femaleCardInfo = new VRSelectManager.SelectCardInfo();
            femaleCardInfo.fileName = winner.FileName;
            femaleCardInfo.status = GetRandomNum(0, 6);//TODO how to transcribe this from char card info?
            femaleCardInfo.resistH = winner.resistH == 1;
            femaleCardInfo.resistPain = winner.resistPain == 1;
            femaleCardInfo.resistAnal = winner.resistAnal == 1;

            return femaleCardInfo;
        }


        public static string GetRandomMale()
        {
            var males = GameCharaFileInfoAssist.CreateCharaFileInfoList(1, true, false, true, false, true, true, true);

            var maleCount = males.Count;
            var winnerNum = GetRandomNum(0, maleCount-1);
            var winner = males[winnerNum];

            return winner.FileName;
        }

    }

}