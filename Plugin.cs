using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;

namespace GTFO
{
    [BepInPlugin("com.dvize.GTFO", "dvize.GTFO", "1.0.6")]
    public class GTFOPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enabledPlugin;
        public static ConfigEntry<float> distanceLimit;
        public static ConfigEntry<KeyboardShortcut> extractKeyboardShortcut;
        public static ConfigEntry<KeyboardShortcut> questKeyboardShortcut;
        public static ConfigEntry<float> displayTime;

        private void Awake()
        {

            enabledPlugin = Config.Bind(
                "Main Settings",
                "Enable",
                true,
                "Enable the plugin to show with extracts/quests objectives");

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

            new NewGamePatch().Enable();
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
}
