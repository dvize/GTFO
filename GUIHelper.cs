using System.Collections.Generic;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace GTFO
{
    public static class GUIHelper
    {
        private static GUIStyle style;
        private static Vector3 screenPosition;

        internal static void DrawLabels(bool displayActive, Vector3[] extractPositions, float[] extractDistances, string[] extractNames, Player player)
        {
            if (displayActive)
            {
                if (style == null)
                {
                    style = new GUIStyle();
                    style.normal.textColor = Color.white;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 18;
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
        }
        internal static void UpdateLabels()
        {
            List<ExfiltrationPoint> enabledPoints = ExtractManager.GetEnabledExfiltrationPoints();
            for (int i = 0; i < enabledPoints.Count; i++)
            {
                ExtractManager.extractPositions[i] = enabledPoints[i].transform.position;
                ExtractManager.extractNames[i] = enabledPoints[i].Settings.Name.Localized();
                ExtractManager.extractDistances[i] = Vector3.Distance(ExtractManager.extractPositions[i], GTFOComponent.player.Position);
            }
        }
    }
}
