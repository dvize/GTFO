using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;

namespace GTFO
{
    [BepInPlugin("com.dvize.GTFO", "dvize.GTFO", "1.0.3")]
    public class GTFOPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enabledPlugin;
        public static ConfigEntry<float> distanceLimit;
        public static ConfigEntry<KeyboardShortcut> keyboardShortcut;
        public static ConfigEntry<float> displayTime;

        private void Awake()
        {

            enabledPlugin = Config.Bind(
                "Main Settings",
                "Enable",
                true,
                "Enable the plugin to show with extracts dialog");

            distanceLimit = Config.Bind(
                "Main Settings",
                "Distance Limit",
                300f,
                "Show Extracts at a Maximum Distance of Up To");

            keyboardShortcut = Config.Bind(
                "Main Settings",
                "Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.O),
                "Toggle Extracts Dialog");

            displayTime = Config.Bind(
                "Main Settings",
                "Display Time",
                5f,
                "Time to Display Extracts Dialog");

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
