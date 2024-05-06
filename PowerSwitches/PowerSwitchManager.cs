using System;
using System.Collections.Generic;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using UnityEngine;

namespace GTFO
{
    internal static class PowerSwitchManager
    {
        internal static List<Switch> powerSwitches = new List<Switch>();
        private static GameWorld _gameWorld;
        public static void Initialize()
        {
            _gameWorld = GTFOComponent.gameWorld ?? throw new ArgumentNullException(nameof(GTFOComponent.gameWorld));
            powerSwitches.Clear();

            SetupInitialPowerSwitches();
        }
        public static void Deinitialize()
        {
            // Clear lists of powerSwitches
            powerSwitches.Clear();

            GTFOComponent.Logger.LogInfo("PowerSwitchManager has been deinitialized and resources cleared.");
        }
        private static void SetupInitialPowerSwitches()
        {
            //doesn't matter if scav or pmc run

            // Find all switches in the scene
            var AllSwitches = GameObject.FindObjectsOfType<Switch>();

            // Iterate over all found switches
            foreach (Switch @switch in AllSwitches)
            {
                // Check if the switch can be interacted with
                if (@switch.HasAuthority && 
                    @switch.Operatable && 
                    @switch.PreviousSwitch == null && // trying to get rid of extra switches everywhere. get first in the chain of switches that player interacts with.
                    @switch.DoorState == EDoorState.Shut && //only want switches that are closed at start
                    !@switch.name.ToLower().Contains("reset") && //janky way to do this. reset switch on reserve
                    !@switch.name.ToLower().Contains("node")  //try to fix interchange
                    )
                {
                    powerSwitches.Add(@switch);
                }
            }

            GTFOComponent.Logger.LogWarning($"Found {powerSwitches.Count} power switches in the scene.");
        }

        internal static bool currentlyTriggered(Switch switchObj)
        {
            if(switchObj.DoorState == EDoorState.Open || switchObj.DoorState == EDoorState.Interacting)
            {
                return true;
            }

            return false;
        }
    }
}
