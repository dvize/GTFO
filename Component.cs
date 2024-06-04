using System.Collections;
using System.Linq;
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
    internal static bool ExtractAndSwitchDisplayActive;
    internal static bool questDisplayActive;
    internal static QuestManager questManager;

    private void Awake()
    {
        if (Logger == null)
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(GTFOComponent));
    }

    private void Start()
    {
        player = gameWorld.MainPlayer;
        questManager = new QuestManager();
        ExtractAndSwitchDisplayActive = false;
        questDisplayActive = false;

        ExtractManager.Initialize();
        questManager.Initialize(ref gameWorld, ref player);
        PowerSwitchManager.Initialize();
    }

    private void Update()
    {
        if (!GTFOPlugin.enabledPlugin.Value)
            return;

        if (IsKeyPressed(GTFOPlugin.extractKeyboardShortcut.Value) && !ExtractAndSwitchDisplayActive)
        {
            ToggleExtractionPointsDisplay(true);
        }

        if (IsKeyPressed(GTFOPlugin.questKeyboardShortcut.Value) && !questDisplayActive)
        {
            ToggleQuestPointsDisplay(true);
        }

        GUIHelper.UpdateLabels();
    }

    private void ToggleQuestPointsDisplay(bool display)
    {
        questDisplayActive = display;
        if (display)
        {
            StartCoroutine(HideQuestPointsAfterDelay(GTFOPlugin.displayTime.Value));
        }
    }

    private void ToggleExtractionPointsDisplay(bool display)
    {
        ExtractAndSwitchDisplayActive = display;
        if (display)
        {
            StartCoroutine(HideExtractPointsAfterDelay(GTFOPlugin.displayTime.Value));
        }
    }

    private IEnumerator HideQuestPointsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideQuestPoints();
    }

    private void HideQuestPoints()
    {
        questDisplayActive = false;
    }

    private IEnumerator HideExtractPointsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideExtractionPoints();
    }

    private void HideExtractionPoints()
    {
        ExtractAndSwitchDisplayActive = false;
    }


    private void OnGUI()
    {
        if (ExtractAndSwitchDisplayActive)
        {
            GUIHelper.DrawExtracts(ExtractAndSwitchDisplayActive, ExtractManager.extractPositions, ExtractManager.extractDistances, ExtractManager.extractNames, player);
            
            if (GTFOPlugin.showPowerSwitches.Value)
                GUIHelper.DrawPowerSwitches(ExtractAndSwitchDisplayActive);
        }

        if (questDisplayActive)
        {
            GUIHelper.DrawQuests(questDisplayActive);
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
        if (!UnityInput.Current.GetKeyDown(key.MainKey)) return false;

        return key.Modifiers.All(modifier => UnityInput.Current.GetKey(modifier));
    }

    private void OnDestroy()
    {
        if (Logger != null)
        {
            Logger.LogInfo("GTFOComponent is being destroyed");
        }

        // Disable any active displays to ensure they don't persist in the UI
        if (ExtractAndSwitchDisplayActive)
        {
            HideExtractionPoints();
        }

        if (questDisplayActive)
        {
            HideQuestPoints();
        }

        // Deinitialize any managers or services that were initialized
        ExtractManager.Deinitialize();
        PowerSwitchManager.Deinitialize();
        if (questManager != null)
        {
            questManager.Deinitialize();
        }

        // Remove the log source if it was created
        if (Logger != null)
        {
            Logger.Dispose();
            Logger = null;
        }

        // Clear static references to prevent memory leaks
        gameWorld = null;
        player = null;
        questManager = null;

        //Clear quest selection from menu since we want to refresh
        GTFOPlugin.Instance.ClearQuestDropdownInConfig();

    }


}