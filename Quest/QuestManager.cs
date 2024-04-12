using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using GTFO;

namespace GTFO
{
    internal class QuestManager
    {

        internal QuestDataService questDataService;

        public void Initialize(ref GameWorld gameWorld, ref Player player)
        {
            questDataService = new QuestDataService(ref gameWorld, ref player);
            SetupInitialQuests();
        }
        public void Deinitialize()
        {
            GTFOComponent.Logger.LogInfo("Deinitializing QuestManager.");

            // Perform any cleanup required for the quest data service
            if (questDataService != null)
            {
                questDataService.Cleanup();
                questDataService = null;
            }
        }
        internal void SetupInitialQuests()
        {
            if (!Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                GTFOComponent.Logger.LogInfo("Calling Reload Quest Data from SetupInitial Quests");
                questDataService.ReloadQuestData(ZoneDataHelper.GetAllTriggers());
            }
            else
            {
                GTFOComponent.Logger.LogInfo("Not calling Setup Quests as its a SCAV Raid.");
            }
        }
        internal void OnQuestsChanged(TriggerWithId[] allTriggers)
        {
            if (!Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                questDataService.ReloadQuestData(allTriggers);
            }
        }
    }
}
