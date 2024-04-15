using System.Reflection;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

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
            if (Singleton<GameWorld>.Instance == null)
            {
                Debug.LogError("SpecialPlaceVisited Postfix: GameWorld instance is null.");
                return;
            }

            if (Singleton<GameWorld>.Instance.TryGetComponent<GTFOComponent>(out GTFOComponent gtfo))
            {
                if (gtfo != null && id != null)
                {
                    if (GTFOComponent.questManager != null)
                    {
                        GTFOComponent.questManager.OnConditionalQuestsChanged(id);
                    }
                    else
                    {
                        Debug.LogError("SpecialPlaceVisited Postfix: QuestManager is null within GTFOComponent.");
                    }
                }
                else
                {
                    Debug.LogError($"SpecialPlaceVisited Postfix: Either 'gtfo' is null ({gtfo == null}) or 'id' is null ({id == null}).");
                }
            }
            else
            {
                Debug.LogError("SpecialPlaceVisited Postfix: Failed to retrieve GTFOComponent from GameWorld.");
            }

        }
    }
}
