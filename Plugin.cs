using System;
using System.Collections.Generic;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using dvize.GTFO.Quest;
using EFT;
using UnityEngine;

namespace GTFO
{
    [BepInPlugin("com.dvize.GTFO", "dvize.GTFO", "1.1.1")]
    public class GTFOPlugin : BaseUnityPlugin
    {
        public static GTFOPlugin Instance
        {
            get; private set;
        }

        internal static ConfigEntry<bool> enabledPlugin;
        internal static ConfigEntry<float> extractDistanceLimit;
        internal static ConfigEntry<float> questDistanceLimit;
        internal static ConfigEntry<KeyboardShortcut> extractKeyboardShortcut;
        internal static ConfigEntry<KeyboardShortcut> questKeyboardShortcut;
        internal static ConfigEntry<float> displayTime;
        internal static ConfigEntry<bool> showOnlyNecessaryObjectives;

        internal static ConfigEntry<int> descriptionMaxCharacterLimit;
        internal static ConfigEntry<int> descriptionWordWrapCharacterLimit;
        internal static ConfigEntry<int> TextSize;
        internal static ConfigEntry<Color> extractStyleColor;
        internal static ConfigEntry<Color> questStyleColor;

        internal static ConfigEntry<string> questSelection;
        internal string[] questValues = new string[] { };
        public GTFOPlugin()
        {
            Instance = this;
        }
        private void Awake()
        {

            // Main
            enabledPlugin = Config.Bind(
                "1. Main Settings",
                "Enable Mod",
                true,
                new ConfigDescription("Enable the plugin to show with extracts/quests objectives", null, new ConfigurationManagerAttributes { Order = 5 })
            );

            displayTime = Config.Bind(
                "1. Main Settings",
                "Display Time",
                10f,
                new ConfigDescription("Amount of Time to Display Objective Points", new AcceptableValueRange<float>(1f, 60f), new ConfigurationManagerAttributes { Order = 4 })
            );

            descriptionMaxCharacterLimit = Config.Bind(
                "1. Main Settings",
                "Description Max Character Limit",
                50,
                new ConfigDescription("How long the description should be displayed before truncated.\nNeeds to be higher than Description Word Wrap Character Limit", new AcceptableValueRange<int>(30, 100), new ConfigurationManagerAttributes { Order = 3 })
            );

            descriptionWordWrapCharacterLimit = Config.Bind(
                "1. Main Settings",
                "Description Word Wrap Character Limit",
                25,
                new ConfigDescription("How many characters wide the description can display", new AcceptableValueRange<int>(10, 50), new ConfigurationManagerAttributes { Order = 2 })
            );

            TextSize = Config.Bind(
                "1. Main Settings",
                "Text Font Size",
                14,
                new ConfigDescription("Size of the text for display.", new AcceptableValueRange<int>(10, 24), new ConfigurationManagerAttributes { Order = 1 })
            );

            // Extract Related
            extractKeyboardShortcut = Config.Bind(
                "2. Extracts",
                "Extract Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.O),
                new ConfigDescription("Toggle Extracts Display", null, new ConfigurationManagerAttributes { Order = 3 })
            );

            extractStyleColor = Config.Bind(
                "2. Extracts",
                "Extract Style Color",
                Color.green,
                new ConfigDescription("Text color for Extracts.", null, new ConfigurationManagerAttributes { Order = 2 })
            );

            extractDistanceLimit = Config.Bind(
                "2. Extracts",
                "Extract Distance Limit",
                500f,
                new ConfigDescription("Show Extracts at a Maximum Distance of Up To", new AcceptableValueRange<float>(100f, 1000f), new ConfigurationManagerAttributes { Order = 1 })
            );

            // Quest Related
            questKeyboardShortcut = Config.Bind(
                "3. Quests",
                "Quest Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.P),
                new ConfigDescription("Toggle Quest Display", null, new ConfigurationManagerAttributes { Order = 4 }));

            questStyleColor = Config.Bind(
                "3. Quests",
                "Quest Style Color",
                Color.red,
                new ConfigDescription("Text color for Quests.", null, new ConfigurationManagerAttributes { Order = 3})
            );

            showOnlyNecessaryObjectives = Config.Bind(
                "3. Quests",
                "Display Only Necessary Quest Conditions",
                false,
                new ConfigDescription("Only Display Necessary Quest Conditions", null, new ConfigurationManagerAttributes { Order = 2 })
            );

            questDistanceLimit = Config.Bind(
                "3. Quests",
                "Quest Distance Limit",
                500f,
                new ConfigDescription("Show Quests at a Maximum Distance of Up To", new AcceptableValueRange<float>(100f, 1000f), new ConfigurationManagerAttributes { Order = 1})
            );

            new NewGamePatch().Enable();
            new TryNotifyConditionChangedPatch().Enable();
            new SpecialPlaceVisitedPatch().Enable();

            // Subscribe to setting changes
            TextSize.SettingChanged += OnStyleSettingChanged;
            extractStyleColor.SettingChanged += OnStyleSettingChanged;
            questStyleColor.SettingChanged += OnStyleSettingChanged;
        }
        private static void OnStyleSettingChanged(object sender, EventArgs e)
        {
            GUIHelper.UpdateStyles();
        }

        internal void RebindDropDown(List<string> questsList)
        {
            var questsArray = questsList.ToArray();
            questSelection = Config.Bind(
                "3. Quests",
                "Quest Selection",
                "All",
                new ConfigDescription("Select which quests to display.Options: All, or Specific while in-raid",
                new AcceptableValueList<string>(questsArray),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));
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

