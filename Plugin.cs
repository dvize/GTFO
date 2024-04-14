using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using dvize.GTFO.Quest;
using EFT;
using UnityEngine;

namespace GTFO
{
    [BepInPlugin("com.dvize.GTFO", "dvize.GTFO", "1.1.0")]
    public class GTFOPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> enabledPlugin;
        internal static ConfigEntry<float> distanceLimit;
        internal static ConfigEntry<KeyboardShortcut> extractKeyboardShortcut;
        internal static ConfigEntry<KeyboardShortcut> questKeyboardShortcut;
        internal static ConfigEntry<float> displayTime;
        internal static ConfigEntry<bool> showOnlyNecessaryObjectives;
        internal static ConfigEntry<int> descriptionMaxCharacterLimit;
        internal static ConfigEntry<int> descriptionWordWrapCharacterLimit;

        private void Awake()
        {

            enabledPlugin = Config.Bind(
                "Main Settings",
                "Enable Mod",
                true,
                "Enable the plugin to show with extracts/quests objectives");

            showOnlyNecessaryObjectives = Config.Bind(
                "Main Settings",
                "Display Only Necessary Quest Conditions",
                false,
                "Only Display Necessary Quest Conditions");

            distanceLimit = Config.Bind(
                "Main Settings",
                "Distance Limit",
                500f,
                "Show Extracts at a Maximum Distance of Up To");

            extractKeyboardShortcut = Config.Bind(
                "Main Settings",
                "Extract Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.O),
                "Toggle Extracts Display");

            questKeyboardShortcut = Config.Bind(
                "Main Settings",
                "Quest Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.P),
                "Toggle Quest Display");

            displayTime = Config.Bind(
                "Main Settings",
                "Display Time",
                10f,
                "Amount of Time to Display Objective Points");

            descriptionMaxCharacterLimit = Config.Bind(
                "Main Settings",
                "Description Max Character Limit",
                50,
                "How long the description should be displayed before truncated.\nNeeds to be higher than Description Word Wrap Character Limit");

            descriptionWordWrapCharacterLimit = Config.Bind(
                "Main Settings",
                "Description Word Wrap Character Limit",
                25,
                "How many wide the description can display");

            new NewGamePatch().Enable();
            new TryNotifyConditionChangedPatch().Enable();
            new SpecialPlaceVisitedPatch().Enable();

        }


    }
}

//re-initializes each new game
internal class NewGamePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

    [PatchPrefix]
    public static void PatchPrefix()
    {
        GTFOComponent.Enable();
    }
}

