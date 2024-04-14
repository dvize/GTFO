using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;

namespace dvize.GTFO.Quest
{
    internal class SpecialPlaceVisitedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {

            return AccessTools.Method(typeof(Player), nameof(Player.SpecialPlaceVisited));
        }

        [PatchPostfix]
        public static void Postfix(ref string id, ref int experience)
        {
            if (id != null && GTFOComponent.questManager != null)
            {
                GTFOComponent.questManager.OnConditionalQuestsChanged(id);
            }
            else
            {
                GTFOComponent.Logger.LogError("SpecialPlaceVisitedPatch: id is null or QuestManager is null.");
            }

        }
    }
}
