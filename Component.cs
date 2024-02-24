using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using GTFO;
using UnityEngine;

public class GTFOComponent : MonoBehaviour
{
    internal static ManualLogSource Logger;
    internal static GameWorld gameWorld;
    internal static Player player;
    internal static bool displayActive;

    private void Awake()
    {
        if (Logger == null)
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(GTFOComponent));
    }

    private void Start()
    {
        player = gameWorld.MainPlayer;

        displayActive = false;

        ExtractManager.Initialize();
    }

    private void Update()
    {
        if (!GTFOPlugin.enabledPlugin.Value)
            return;

        if (IsKeyPressed(GTFOPlugin.keyboardShortcut.Value) && !displayActive)
        {
            ToggleExtractionPointsDisplay(true);
        }

        GUIHelper.UpdateLabels();
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


    private void OnGUI()
    {
        if (displayActive)
        {
            GUIHelper.DrawLabels(displayActive, ExtractManager.extractPositions, ExtractManager.extractDistances, ExtractManager.extractNames, player);
        }
    }

    public static void Enable()
    {
        if (Singleton<IBotGame>.Instantiated)
        {
            //add component to gameWorld
            gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.gameObject.AddComponent<GTFOComponent>();
            Logger.LogDebug("GTFO Enabled");

        }
    }

    bool IsKeyPressed(KeyboardShortcut key)
    {
        if (!UnityInput.Current.GetKeyDown(key.MainKey))
        {
            return false;
        }
        foreach (var modifier in key.Modifiers)
        {
            if (!UnityInput.Current.GetKey(modifier))
            {
                return false;
            }
        }
        return true;
    }
}