using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using EFT;
using EFT.Interactive;
using EFT.UI.Ragfair;
using EFT.Visual;
using UnityEngine;
using static EFT.SpeedTree.TreeWind;
using static HBAO_Core;

namespace GTFO
{
    public static class GUIHelper
    {
        private static GUIStyle style;
        private static GUIStyle style2;
        private static Vector3 screenPosition;
        private static bool stylesInitialized = false;
        private static Vector2 lastScreenSize = Vector2.zero;

        internal static void EnsureStyles()
        {
            if (!stylesInitialized || ScreenSizeChanged())
            {
                InitializeStyles(); // Initializes or updates styles
                UpdateStyleBasedOnResolution();
                stylesInitialized = true;
                lastScreenSize = new Vector2(Screen.width, Screen.height);
            }
        }
        private static bool ScreenSizeChanged()
        {
            return lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height;
        }

        internal static void InitializeStyles()
        {
            style = new GUIStyle()
            {
                normal = { textColor = Color.green, background = Texture2D.blackTexture },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            style2 = new GUIStyle()
            {
                normal = { textColor = Color.red, background = Texture2D.blackTexture },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            UpdateStyleBasedOnResolution();
        }

        internal static void UpdateStyleBasedOnResolution()
        {
            int baseFontSize = 12;
            float resolutionScalingFactor = Screen.height / 1080f;

            // Dynamically adjust font size based on resolution
            style.fontSize = Mathf.RoundToInt(baseFontSize * resolutionScalingFactor);
            style2.fontSize = Mathf.RoundToInt(baseFontSize * resolutionScalingFactor);
        }
        internal static void DrawExtracts(bool displayActive, Vector3[] extractPositions, float[] extractDistances, string[] extractNames, Player player)
        {
            if (!displayActive)
                return;

            EnsureStyles();

            for (int i = 0; i < extractPositions.Length; i++)
            {
                if (extractDistances[i] > GTFOPlugin.distanceLimit.Value)
                {
                    continue;
                }

                screenPosition = Camera.main.WorldToScreenPoint(extractPositions[i]);

                // Ensure the position is on screen and in front of the camera
                if (screenPosition.z > 0 &&
                    screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                    screenPosition.y >= 0 && screenPosition.y <= Screen.height)
                {
                    // Adjust label size based on resolution
                    float labelWidth = 200 * (Screen.width / 1920f);
                    float labelHeight = 50 * (Screen.height / 1080f);

                    float adjustedY = Screen.height - screenPosition.y - labelHeight; 

                    string label = $"Extract Name: {extractNames[i]}\nDistance: {extractDistances[i]:F2} meters";
                    GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, style);
                }
            }
        }

        internal static void DrawQuests(bool questDisplayActive)
        {
            if (!questDisplayActive)
                return;

            EnsureStyles();

            foreach (QuestData quest in GTFOComponent.questManager.questDataService.QuestObjectives)
            {
                if (GTFOPlugin.showOnlyNecessaryObjectives.Value && !quest.IsNecessary)
                {
                    continue;
                }

                screenPosition = Camera.main.WorldToScreenPoint(new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z));

                // Convert to relative positioning
                float posX = screenPosition.x / Screen.width;
                float posY = (Screen.height - screenPosition.y) / Screen.height;

                if (posX >= 0 && posX <= 1 && posY >= 0 && posY <= 1 && screenPosition.z > 0)
                {
                    // Scale label size based on resolution
                    float labelWidth = 200 * (Screen.width / 1920.0f); // Adjust based on your base resolution
                    float labelHeight = 100 * (Screen.height / 1080.0f);

                    string label = $"Quest Name: {quest.NameText}\nDescription: {quest.Description}\nDistance: {Vector3.Distance(new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z), GTFOComponent.player.Position)}";
                    GUI.Label(new Rect(posX * Screen.width, posY * Screen.height, labelWidth, labelHeight), label, style2);
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
