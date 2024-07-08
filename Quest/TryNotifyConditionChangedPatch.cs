using System.Reflection;
using SPT.Reflection.Patching;
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

            return AccessTools.Method(typeof(GClass3228), nameof(GClass3228.TryNotifyConditionChanged));
        }

        [PatchPostfix]
        public static void Postfix(ref QuestClass quest)
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