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
            GTFOComponent.questManager.OnConditionalQuestsChanged(id);

        }
    }
}
