using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace GTFO
{
    public static class GUIHelper
    {
        private static GUIStyle extractStyle;
        private static GUIStyle questStyle;
        private static bool stylesInitialized = false;
        private static Vector2 lastScreenSize = Vector2.zero;

        internal static void EnsureStyles()
        {
            if (!stylesInitialized || ScreenSizeChanged())
            {
                InitializeStyles();
                stylesInitialized = true;
                lastScreenSize = new Vector2(Screen.width, Screen.height);
            }
        }
        public static void UpdateStyles()
        {
            // Force reinitialization of styles
            stylesInitialized = false;
            EnsureStyles();
        }
        private static bool ScreenSizeChanged()
        {
            return lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height;
        }

        internal static void InitializeStyles()
        {
            extractStyle = new GUIStyle()
            {
                normal = { textColor = GTFOPlugin.extractStyleColor.Value, background = Texture2D.blackTexture },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = GTFOPlugin.TextSize.Value
            };

            questStyle = new GUIStyle()
            {
                normal = { textColor = GTFOPlugin.questStyleColor.Value, background = Texture2D.blackTexture },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = GTFOPlugin.TextSize.Value
            };
        }

        internal static void DrawExtracts(bool displayActive, Vector3[] extractPositions, float[] extractDistances, string[] extractNames, Player player)
        {
            if (!displayActive)
                return;

            EnsureStyles();

            float pitchAdjustmentFactor = CalculatePitchAdjustmentFactor(Camera.main.transform.eulerAngles.x);

            for (int i = 0; i < extractPositions.Length; i++)
            {
                if (extractDistances[i] > GTFOPlugin.distanceLimit.Value)
                {
                    continue;
                }

                Vector3 screenPosition = Camera.main.WorldToScreenPoint(extractPositions[i]);

                screenPosition.y += pitchAdjustmentFactor;

                if (IsOnScreen(screenPosition))
                {
                    float scaleFactor = GetSuperSamplingFactor();
                    float labelWidth = 200 * scaleFactor;
                    float labelHeight = 50 * scaleFactor;
                    string label = $"Extract Name: {extractNames[i]}\nDistance: {extractDistances[i]:F2} meters";

                    // Adjusted Y position taking into account pitch adjustment and screen height
                    float adjustedY = Screen.height - screenPosition.y - labelHeight / 2;
                    GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, extractStyle);
                }
            }
        }


        private static bool IsOnScreen(Vector3 screenPosition)
        {
            return screenPosition.z > 0 &&
                   screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                   screenPosition.y >= 0 && screenPosition.y <= Screen.height;
        }

        private static float GetSuperSamplingFactor()
        {
            var graphicsSettings = Singleton<SharedGameSettingsClass>.Instance.Graphics.Settings;

            if (graphicsSettings.IsDLSSEnabled() || graphicsSettings.IsFSR2Enabled())
            {
                return graphicsSettings.SuperSamplingFactor;
            }
            else
            {
                return 1.0f;
            }
        }
        internal static void DrawQuests(bool questDisplayActive)
        {
            if (!questDisplayActive)
                return;

            if (!Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                EnsureStyles();

                // Check if the main camera is available
                if (Camera.main == null)
                    return;

                Vector3 cameraEulerAngles = Camera.main.transform.eulerAngles;
                float pitchAdjustmentFactor = CalculatePitchAdjustmentFactor(cameraEulerAngles.x);

                if (GTFOComponent.questManager?.questDataService?.QuestObjectives != null)
                {
                    foreach (QuestData quest in GTFOComponent.questManager.questDataService.QuestObjectives)
                    {
                        if (quest == null || GTFOComponent.player == null)
                            continue;

                        if (GTFOPlugin.showOnlyNecessaryObjectives.Value && !quest.IsNecessary)
                            continue;

                        // Safely handle quest.Location and null strings
                        Vector3 questPosition = quest.Location != null ? new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z) : Vector3.zero;
                        Vector3 screenPosition = Camera.main.WorldToScreenPoint(questPosition);

                        // Adjust y based on pitch
                        screenPosition.y += pitchAdjustmentFactor;

                        if (IsOnScreen(screenPosition))
                        {
                            float scaleFactor = GetSuperSamplingFactor();
                            float labelWidth = 200 * scaleFactor;
                            float labelHeight = 100 * scaleFactor;
                            float adjustedY = Screen.height - screenPosition.y - labelHeight / 2;

                            // Ensure quest.NameText and quest.Description are not null
                            string nameText = quest.NameText ?? "Unknown Name";
                            string description = quest.Description ?? "No description available";

                            // Truncate the description at 50 characters
                            if (description.Length > GTFOPlugin.descriptionMaxCharacterLimit.Value)
                            {
                                description = description.Substring(0, GTFOPlugin.descriptionMaxCharacterLimit.Value);
                            }

                            // Word wrap the description at 20 characters
                            if (description.Length > GTFOPlugin.descriptionWordWrapCharacterLimit.Value)
                            {
                                int wrapPosition = description.Substring(0, GTFOPlugin.descriptionWordWrapCharacterLimit.Value).LastIndexOf(' ');
                                if (wrapPosition == -1)
                                {
                                    wrapPosition = GTFOPlugin.descriptionWordWrapCharacterLimit.Value;
                                }
                                description = description.Insert(wrapPosition, "\n");
                            }



                            string label = $"Quest Name: {nameText}\nDescription: {description}\nDistance: {Vector3.Distance(questPosition, GTFOComponent.player.Position):F2} meters";

                            GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, questStyle);
                        }
                    }
                }
            }
        }

        private static float CalculatePitchAdjustmentFactor(float pitchAngle)
        {
            // Normalize pitch angle to [0, 360]
            float normalizedPitch = pitchAngle % 360;
            if (normalizedPitch < 0) normalizedPitch += 360;

            if (normalizedPitch > 180)
            {
                normalizedPitch = 360 - normalizedPitch;
            }

            float adjustment = 0.5f * (normalizedPitch - 90);

            return adjustment;
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
