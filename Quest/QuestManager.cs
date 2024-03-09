using System.Collections.Generic;
using Comfort.Common;
using EFT.Interactive;
using EFT.UI;
using GTFO;

namespace GTFO
{
    internal class QuestManager
    {
        internal QuestDataService questDataService = new QuestDataService();
        public void Initialize()
        {
            SetupInitialQuests();
        }
        public void SetupInitialQuests()
        {
            GTFOComponent.Logger.LogInfo("Calling Reload Quest Data from SetupInitial Quests");
            questDataService.ReloadQuestData(ZoneDataHelper.GetAllTriggers());
        }
        public void OnQuestsChanged(TriggerWithId[] allTriggers)
        {
            questDataService.ReloadQuestData(allTriggers);
        }
    }
}
