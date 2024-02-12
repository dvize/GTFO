using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using GTFO;
using UnityEngine;

public class GTFOComponent : MonoBehaviour
{
    private static GameWorld gameWorld;
    private List<ExfiltrationPoint> enabledExfiltrationPoints = new List<ExfiltrationPoint>();
    private List<ScavExfiltrationPoint> enabledScavExfiltrationPoints = new List<ScavExfiltrationPoint>();
    private Player player;
    private bool displayActive;
    private GUIStyle style;
    private Vector3[] extractPositions;
    private string[] extractNames;
    private float[] extractDistances;
    private Vector3 screenPosition;

    protected static ManualLogSource Logger
    {
        get; private set;
    }
    public GTFOComponent()
    {
        if (Logger == null)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(GTFOComponent));
        }
    }

    private void Start()
    {
        player = gameWorld.MainPlayer;
        enabledExfiltrationPoints.Clear();
        enabledScavExfiltrationPoints.Clear();

        SetupInitialExtracts();
    }

    private void SetupInitialExtracts()
    {
        //check if we are in a scav run
        if (Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
        {
            foreach (ScavExfiltrationPoint scavExfiltrationPoint in gameWorld.ExfiltrationController.ScavExfiltrationPoints)
            {
                //check if enabled and if assigned to our player scav
                if (scavExfiltrationPoint.isActiveAndEnabled && scavExfiltrationPoint.InfiltrationMatch(player))
                {
                    enabledScavExfiltrationPoints.Add(scavExfiltrationPoint);
                }
            }

            Logger.LogWarning("Enabled Scav Exfiltration Points: " + enabledScavExfiltrationPoints.Count);

            extractDistances = new float[enabledScavExfiltrationPoints.Count];
            extractPositions = new Vector3[enabledScavExfiltrationPoints.Count];
            extractNames = new string[enabledScavExfiltrationPoints.Count];

            return;
        }
        else 
        {
            foreach (ExfiltrationPoint exfiltrationPoint in gameWorld.ExfiltrationController.ExfiltrationPoints)
            {
                //check if enabled and if assigned to our player
                if (exfiltrationPoint.isActiveAndEnabled && exfiltrationPoint.InfiltrationMatch(player))
                {
                    enabledExfiltrationPoints.Add(exfiltrationPoint);
                }
            }

            Logger.LogWarning("Enabled Exfiltration Points: " + enabledExfiltrationPoints.Count);

            extractDistances = new float[enabledExfiltrationPoints.Count];
            extractPositions = new Vector3[enabledExfiltrationPoints.Count];
            extractNames = new string[enabledExfiltrationPoints.Count];
        }
    }

    private void Update()
    {
        if (GTFOPlugin.enabledPlugin.Value == false)
        {
            return;
        }

        if (GTFOPlugin.keyboardShortcut.Value.IsDown() && !displayActive)
        {
            ToggleExtractionPointsDisplay(true);
        }

        UpdateLabels();
    }

    private void ToggleExtractionPointsDisplay(bool display)
    {
        displayActive = display;
        if (display)
        {
            StartCoroutine(HideExtractionPointsAfterDelay(GTFOPlugin.displayTime.Value));
        }
        else
        {
            HideExtractionPoints();
        }
    }

    private IEnumerator HideExtractionPointsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideExtractionPoints();
    }

    private void HideExtractionPoints()
    {
        displayActive = false;
    }

    private void UpdateLabels()
    {
        if (Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
        {
            for (int i = 0; i < enabledScavExfiltrationPoints.Count; i++)
            {
                extractPositions[i] = enabledScavExfiltrationPoints[i].transform.position;

                extractNames[i] = enabledScavExfiltrationPoints[i].Settings.Name.Localized();
                extractDistances[i] = Vector3.Distance(extractPositions[i], player.Position);
            }
            return;
        }
        else
        {
            for (int i = 0; i < enabledExfiltrationPoints.Count; i++)
            {
                extractPositions[i] = enabledExfiltrationPoints[i].transform.position;

                extractNames[i] = enabledExfiltrationPoints[i].Settings.Name.Localized();
                extractDistances[i] = Vector3.Distance(extractPositions[i], player.Position);
            }
        }

    }


    private void OnGUI()
    {
        if (displayActive)
        {
            DrawLabels();
        }
    }

    private void DrawLabels()
    {
        if (style is null)
        {
            style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 18;
        }

        for (int i = 0; i < extractPositions.Length; i++)
        {
            //check if distance is less than plugin value
            if (extractDistances[i] > GTFOPlugin.distanceLimit.Value)
            {
                continue;
            }

            screenPosition = Camera.main.WorldToScreenPoint(extractPositions[i]);

            // Check if the label position is within the screen bounds
            if (screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                screenPosition.y >= 0 && screenPosition.y <= Screen.height &&
                screenPosition.z > 0) // Ensure the object is in front of the camera
            {
                string label = $"Extract Name: {extractNames[i]}\nDistance: {extractDistances[i]:F2} meters";
                GUI.Label(new Rect(screenPosition.x, Screen.height - screenPosition.y, 200, 50), label, style);
            }
        }
    }

    public static void Enable()
    {
        if (Singleton<IBotGame>.Instantiated)
        {
            gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.GetOrAddComponent<GTFOComponent>();

            Logger.LogDebug("GTFO Enabled");
        }
    }
}
