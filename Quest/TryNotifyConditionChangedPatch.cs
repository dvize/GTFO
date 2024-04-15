using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

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
            if (Singleton<GameWorld>.Instance == null)
            {
                Debug.LogError("TryNotifyConditionChanged Postfix: GameWorld instance is null.");
                return;
            }

            if (Singleton<GameWorld>.Instance.TryGetComponent<GTFOComponent>(out GTFOComponent gtfo))
            {
                if (gtfo != null && quest != null)
                {
                    if (GTFOComponent.questManager != null)
                    {
                        GTFOComponent.questManager.OnQuestsChanged(quest);
                    }
                    else
                    {
                        Debug.LogError("TryNotifyConditionChanged Postfix: QuestManager is null within GTFOComponent.");
                    }
                }
                else
                {
                    Debug.LogError($"TryNotifyConditionChanged Postfix: Either 'gtfo' is null ({gtfo == null}) or 'quest' is null ({quest == null}).");
                }
            }
            else
            {
                Debug.LogError("TryNotifyConditionChanged Postfix: Failed to retrieve GTFOComponent from GameWorld.");
            }


        }
    }
}