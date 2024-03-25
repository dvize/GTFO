using System;
using System.Collections.Generic;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace GTFO
{
    public static class GUIHelper
    {
        private static GUIStyle style;
        private static GUIStyle style2;
        private static Vector3 screenPosition;

        internal static void DrawExtracts(bool displayActive, Vector3[] extractPositions, float[] extractDistances, string[] extractNames, Player player)
        {
            if (!displayActive)
                return;

            if (style == null)
            {
                style = new GUIStyle();
                style.normal.textColor = Color.green;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.background = Texture2D.blackTexture;
                style.fontSize = 16;
            }

            for (int i = 0; i < extractPositions.Length; i++)
            {
                if (extractDistances[i] > GTFOPlugin.distanceLimit.Value)
                {
                    continue;
                }

                screenPosition = Camera.main.WorldToScreenPoint(extractPositions[i]);

                if (screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                    screenPosition.y >= 0 && screenPosition.y <= Screen.height &&
                    screenPosition.z > 0)
                {
                    string label = $"Extract Name: {extractNames[i]}\nDistance: {extractDistances[i]:F2} meters";
                    GUI.Label(new Rect(screenPosition.x, Screen.height - screenPosition.y, 200, 50), label, style);
                }
            }

        }

        internal static void DrawQuests(bool questDisplayActive)
        {
            if (!questDisplayActive)
                return;

            if (style2 == null)
            {
                style2 = new GUIStyle();
                style2.normal.textColor = Color.red;
                style2.normal.background = Texture2D.blackTexture;
                style2.fontStyle = FontStyle.Bold;
                style2.alignment = TextAnchor.MiddleCenter;
                style2.fontSize = 14;
            }

            foreach (QuestData quest in GTFOComponent.questManager.questDataService.QuestObjectives)
            {
                if (GTFOPlugin.showOnlyNecessaryObjectives.Value)
                {
                    if (!quest.IsNecessary)
                    {
                        continue;
                    }
                }

                screenPosition = Camera.main.WorldToScreenPoint(new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z));

                if (screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                    screenPosition.y >= 0 && screenPosition.y <= Screen.height &&
                    screenPosition.z > 0)
                {
                    string label = $"Quest Name: {quest.NameText}\nDescription: {quest.Description}\nDistance: {Vector3.Distance(new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z), GTFOComponent.player.Position)}";
                    GUI.Label(new Rect(screenPosition.x, Screen.height - screenPosition.y, 200, 100), label, style2);
                }
            }

        }
        internal static void UpdateLabels()
        {
            if (!Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                var enabledPoints = ExtractManager.GetEnabledExfiltrationPoints();
                SetUpdateLabelsInfo(enabledPoints, (ExfiltrationPoint point) => point.transform.position, (ExfiltrationPoint point) => point.Settings.Name.Localized());
            }
            else
            {
                var enabledPoints = ExtractManager.GetEnabledScavExfiltrationPoints();
                SetUpdateLabelsInfo(enabledPoints, (ScavExfiltrationPoint point) => point.transform.position, (ScavExfiltrationPoint point) => point.Settings.Name.Localized());
            }
        }

        private static void SetUpdateLabelsInfo<T>(List<T> enabledPoints, Func<T, Vector3> getPosition, Func<T, string> getName)
        {
            for (int i = 0; i < enabledPoints.Count; i++)
            {
                ExtractManager.extractPositions[i] = getPosition(enabledPoints[i]);
                ExtractManager.extractNames[i] = getName(enabledPoints[i]);
                ExtractManager.extractDistances[i] = Vector3.Distance(ExtractManager.extractPositions[i], GTFOComponent.player.Position);
            }
        }
    }
}
