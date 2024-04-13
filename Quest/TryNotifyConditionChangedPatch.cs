using System;
using System.Reflection;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Interactive;
using HarmonyLib;

namespace GTFO
{
    public class TryNotifyConditionChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {

            return AccessTools.Method(typeof(GClass3205), nameof(GClass3205.TryNotifyConditionChanged));
        }

        [PatchPostfix]
        public static void Postfix(ref GClass1249 quest)
        {
            GTFOComponent.questManager.OnQuestsChanged(quest);

        }
    }
}