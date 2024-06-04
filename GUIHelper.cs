using System;
using System.Collections.Generic;
using System.IO;
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
        private static Texture2D extractIcon;
        private static Texture2D questIcon;
        private static Texture2D powerIcon;
        private static Texture2D waveTexture;

        private static bool stylesInitialized = false;
        private static Vector2 lastScreenSize = Vector2.zero;

        private static float pulsationIntensity = 0.5f; 
        private static float pulsationSpeed = 2f; 


        internal static void LoadImages()
        {
            // Assuming DLL is located in the same directory as the running executable
            string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            extractIcon = LoadTexture(Path.Combine(basePath, "extractIcon.png"));
            questIcon = LoadTexture(Path.Combine(basePath, "questIcon.png"));
            powerIcon = LoadTexture(Path.Combine(basePath, "powerIcon.png"));
            waveTexture = LoadTexture(Path.Combine(basePath, "waveTexture.png"));
        }

        private static Texture2D LoadTexture(string filePath)
        {
            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);

                if (ImageConversion.LoadImage(texture, fileData))
                {
                    return texture;
                }
            }
            return null;
        }

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

            for (int i = 0; i < extractPositions.Length; i++)
            {
                if (extractDistances[i] > GTFOPlugin.extractDistanceLimit.Value)
                {
                    continue;
                }

                Vector3 viewportPosition = Camera.main.WorldToViewportPoint(extractPositions[i]);

                if (IsInViewport(viewportPosition))
                {
                    float scaleFactor = GetSuperSamplingFactor();
                    float labelWidth = 200 * scaleFactor;
                    float labelHeight = 50 * scaleFactor;

                    // Convert viewport position back to screen coordinates for drawing
                    Vector3 screenPosition = new Vector3(
                        viewportPosition.x * Screen.width,
                        (1 - viewportPosition.y) * Screen.height,
                        viewportPosition.z);

                    if (GTFOPlugin.showIconsOnlyInsteadOfText.Value)
                    {
                        Texture2D icon = extractIcon;
                        if (icon != null)
                        {
                            float iconSize = GTFOPlugin.iconSize.Value * scaleFactor;

                            // Calculate icon position
                            Rect iconRect = new Rect(screenPosition.x - iconSize / 2, screenPosition.y - iconSize / 2, iconSize, iconSize);
                            GUI.DrawTexture(iconRect, extractIcon);

                            string label = $"Distance: {extractDistances[i]:F2} meters";
                            if (!GTFOPlugin.showPrefixes.Value)
                            {
                                label = $"{extractDistances[i]:F2} meters";
                            }


                            // Position the label directly below the icon
                            float adjustedY = screenPosition.y + iconSize / 2;

                            // Draw label
                            GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, extractStyle);
                        }
                    }
                    else
                    {
                        // Draw text label
                        string label = $"Extract Name: {extractNames[i]}\nDistance: {extractDistances[i]:F2} meters";
                        if (!GTFOPlugin.showPrefixes.Value)
                        {
                            label = $"{extractNames[i]}\n{extractDistances[i]:F2} meters";
                        }

                        float adjustedY = screenPosition.y - labelHeight / 2;
                        GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, extractStyle);
                    }
                }
            }
        }

        internal static void DrawPowerSwitches(bool displayActive)
        {
            if (!displayActive)
                return;

            EnsureStyles();

            foreach (Switch @switch in PowerSwitchManager.powerSwitches)
            {
                if (@switch == null || GTFOComponent.player == null)
                    continue;

                Vector3 switchPosition = @switch.transform.position;
                float switchDistance = Vector3.Distance(switchPosition, GTFOComponent.player.Position);

                //check distance
                if ( switchDistance > GTFOPlugin.extractDistanceLimit.Value)
                    continue;

                Vector3 viewportPosition = Camera.main.WorldToViewportPoint(switchPosition);
                
                if (IsInViewport(viewportPosition))
                {
                    float scaleFactor = GetSuperSamplingFactor();
                    float labelWidth = 200 * scaleFactor;
                    float labelHeight = 50 * scaleFactor;

                    // Convert viewport position back to screen coordinates for drawing
                    Vector3 screenPosition = new Vector3(
                        viewportPosition.x * Screen.width,
                        (1 - viewportPosition.y) * Screen.height,
                        viewportPosition.z);

                    if (GTFOPlugin.showIconsOnlyInsteadOfText.Value)
                    {
                        Texture2D icon = powerIcon;
                        if (icon != null)
                        {
                            float iconSize = GTFOPlugin.iconSize.Value * scaleFactor;

                            // Initialize wave size and color properties outside of the condition
                            float waveSize = iconSize * 1.8f;
                            Color waveColor = Color.clear;  

                            if (PowerSwitchManager.currentlyTriggered(@switch))
                            {
                                iconSize = CalculatePulsationScale(iconSize);
                                waveSize = CalculatePulsationScale(waveSize * 1.2f);

                                float alpha = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI);
                                waveColor = new Color(1, 0, 0, alpha);  // Red 
                            }

                            // Draw the wave texture
                            GUI.color = waveColor;
                            Rect waveRect = new Rect(screenPosition.x - waveSize / 2, screenPosition.y - waveSize / 2, waveSize, waveSize);
                            GUI.DrawTexture(waveRect, waveTexture);

                            // Draw the icon on top of the wave
                            GUI.color = Color.white;
                            Rect iconRect = new Rect(screenPosition.x - iconSize / 2, screenPosition.y - iconSize / 2, iconSize, iconSize);
                            GUI.DrawTexture(iconRect, powerIcon);

                            // Reset GUI color
                            GUI.color = Color.white;

                            string label = $"Distance: {switchDistance:F2} meters";

                            // Position the label directly below the icon
                            float adjustedY = screenPosition.y + iconSize / 2;

                            // Draw label
                            GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, extractStyle);

                        }
                    }
                    else
                    {
                        // Draw text label
                        string label = $"PowerSwitch Name: {@switch.name}\nDistance: {switchDistance:F2} meters\nTriggered: {PowerSwitchManager.currentlyTriggered(@switch)}";

                        float adjustedY = screenPosition.y - labelHeight / 2;
                        GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, extractStyle);
                    }
                }
            }
        }
        private static bool IsInViewport(Vector3 viewportPosition)
        {
            return viewportPosition.z > 0 && viewportPosition.x >= 0 && viewportPosition.x <= 1 && viewportPosition.y >= 0 && viewportPosition.y <= 1;
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

                        // Check if quest selection is made in config menu or if default all
                        if (GTFOPlugin.questSelection.Value != "All" && quest.NameText != GTFOPlugin.questSelection.Value)
                            continue;

                        if (GTFOPlugin.showOnlyNecessaryObjectives.Value && !quest.IsNecessary)
                            continue;

                        // Check distance to display quests
                        if (GTFOPlugin.questDistanceLimit.Value < Vector3.Distance(new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z), GTFOComponent.player.Position))
                            continue;

                        Vector3 questPosition = quest.Location != null ? new Vector3((float)quest.Location.X, (float)quest.Location.Y, (float)quest.Location.Z) : Vector3.zero;
                        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(questPosition);
                        viewportPosition.y += pitchAdjustmentFactor / Screen.height; // Adjust y based on pitch and scale

                        if (IsInViewport(viewportPosition))
                        {
                            float scaleFactor = GetSuperSamplingFactor();
                            float iconSize = GTFOPlugin.iconSize.Value * scaleFactor;

                            Vector3 screenPosition = new Vector3(
                                viewportPosition.x * Screen.width,
                                (1 - viewportPosition.y) * Screen.height,
                                viewportPosition.z);

                            if (GTFOPlugin.showIconsOnlyInsteadOfText.Value && questIcon != null)
                            {
                                Texture2D icon = questIcon;
                                if (icon != null)
                                {
                                    Rect iconRect = new Rect(screenPosition.x - iconSize / 2, screenPosition.y - iconSize / 2, iconSize, iconSize);
                                    GUI.DrawTexture(iconRect, questIcon);

                                    string label = $"Distance: {Vector3.Distance(questPosition, GTFOComponent.player.Position):F2} meters";
                                    if (!GTFOPlugin.showPrefixes.Value)
                                    {
                                        label = $"{Vector3.Distance(questPosition, GTFOComponent.player.Position):F2} meters";
                                    }

                                    float labelHeight = 20 * scaleFactor;
                                    float labelWidth = 200 * scaleFactor;

                                    // Position the label directly below the icon
                                    float adjustedY = screenPosition.y + iconSize / 2;

                                    GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, questStyle);
                                }
                            }
                            else
                            {
                                string nameText = quest.NameText ?? "Unknown Name";
                                string description = quest.Description ?? "No description available";
                                if (description.Length > GTFOPlugin.descriptionMaxCharacterLimit.Value)
                                {
                                    description = description.Substring(0, GTFOPlugin.descriptionMaxCharacterLimit.Value);
                                }

                                if (description.Length > GTFOPlugin.descriptionWordWrapCharacterLimit.Value)
                                {
                                    int wrapPosition = description.Substring(0, GTFOPlugin.descriptionWordWrapCharacterLimit.Value).LastIndexOf(' ');
                                    wrapPosition = wrapPosition == -1 ? GTFOPlugin.descriptionWordWrapCharacterLimit.Value : wrapPosition;
                                    description = description.Insert(wrapPosition, "\n");
                                }

                                string label;
                                if (!GTFOPlugin.showPrefixes.Value)
                                {
                                    label = $"{nameText}\n----\n{description}\n----\n{Vector3.Distance(questPosition, GTFOComponent.player.Position):F2} meters";
                                }
                                else
                                {
                                    label = $"Quest Name: {nameText}\nDescription: {description}\nDistance: {Vector3.Distance(questPosition, GTFOComponent.player.Position):F2} meters";
                                }

                                float labelHeight = 100 * scaleFactor;
                                float labelWidth = 200 * scaleFactor;

                                float adjustedY = screenPosition.y - labelHeight / 2;
                                GUI.Label(new Rect(screenPosition.x - labelWidth / 2, adjustedY, labelWidth, labelHeight), label, questStyle);
                            }
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
        private static float CalculatePulsationScale(float baseScale)
        {
            return baseScale * (1 + pulsationIntensity * Mathf.Sin(Time.time * Mathf.PI * pulsationSpeed));
        }
    }
}
