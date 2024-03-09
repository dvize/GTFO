using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;

namespace GTFO
{
    internal class QuestDataService
    {
        public IReadOnlyList<QuestData> QuestMarkers { get; private set; } = Array.Empty<QuestData>();
        private GameWorld gameWorld;
        private Player player;
        public void ReloadQuestData(TriggerWithId[] allTriggers)
        {
            var questMarkerData = new List<QuestData>(32);

            gameWorld = GTFOComponent.gameWorld;
            player = GTFOComponent.player;

            var absQuestController = Traverse.Create(player).Field("_questController").GetValue<AbstractQuestControllerClass>();
            
            //Quests property references gclass3367_0 field
            var quests = Traverse.Create(absQuestController).Field("gclass3367_0").GetValue<GClass3367>();

            var questsList = Traverse.Create(quests).Field("list_1").GetValue<List<QuestDataClass>>();

            var lootItemsList = Traverse.Create(gameWorld).Field("LootItems").Field("list_0").GetValue<List<LootItem>>();

            (string Id, LootItem Item)[] questItems =
                lootItemsList.Where(x => x.Item.QuestItem).Select(x => (x.TemplateId, x)).ToArray();

            GTFOComponent.Logger.LogInfo("QuestsList Count: " + questsList.Count);

            if (questsList != null)
            {
                foreach (QuestDataClass item in questsList)
                {
#if DEBUG
                    GTFOComponent.Logger.LogInfo("In foreach QuestDataClass loop start");
#endif
                    if (item.Status != EQuestStatus.Started)
                    {
                        continue;
                    }

                    var template = item.Template;
                    var nameKey = template.NameLocaleKey;
                    var traderId = template.TraderId;
                    var Conditions = template.Conditions;
                    var availableForFinishConditionsList = template.Conditions[EQuestStatus.AvailableForFinish];

#if DEBUG
                    GTFOComponent.Logger.LogInfo($"NameKey: {nameKey}");
#endif

                    foreach (Condition condition in availableForFinishConditionsList)
                    {
                        // Already have Conditions available for finish, so we can just use them

#if DEBUG
                        GTFOComponent.Logger.LogInfo($"Condition: {condition.id}");
#endif

                        switch (condition)
                        {
                            case ConditionLeaveItemAtLocation location:
                                {
                                    string zoneId = location.zoneId;
                                    IEnumerable<PlaceItemTrigger> zoneTriggers = allTriggers.GetZoneTriggers<PlaceItemTrigger>(zoneId);

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Started Case ConditionLeaveItemAtLocation");
#endif
                                    if (zoneTriggers != null)
                                    {
                                        foreach (PlaceItemTrigger trigger in zoneTriggers)
                                        {
                                            var staticInfo = new QuestData
                                            {
                                                Id = location.id,
                                                Location = ToQuestLocation(trigger.transform.position),
                                                ZoneId = zoneId,
                                                NameText = nameKey.Localized(),
                                                Description = location.id.Localized(),
                                                Trader = TraderIdToName(traderId),
                                            };

                                            questMarkerData.Add(staticInfo);
                                        }
                                    }

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Finished Case ConditionLeaveItemAtLocation");
#endif
                                    break;
                                }
                            case ConditionPlaceBeacon beacon:
                                {
                                    string zoneId = beacon.zoneId;

                                    IEnumerable<PlaceItemTrigger> zoneTriggers = allTriggers.GetZoneTriggers<PlaceItemTrigger>(zoneId);

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Started Case ConditionPlaceBeacon");
#endif

                                    if (zoneTriggers != null)
                                    {
                                        foreach (PlaceItemTrigger trigger in zoneTriggers)
                                        {
                                            var staticInfo = new QuestData
                                            {
                                                Id = beacon.id,
                                                Location = ToQuestLocation(trigger.transform.position),
                                                ZoneId = zoneId,
                                                NameText = nameKey.Localized(),
                                                Description = beacon.id.Localized(),
                                                Trader = TraderIdToName(traderId),
                                            };

                                            questMarkerData.Add(staticInfo);
                                        }
                                    }
#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Finished Case ConditionPlaceBeacon");
#endif

                                    break;
                                }
                            case ConditionFindItem findItem:
                                {

                                    string[] itemIds = findItem.target;
#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Started Case ConditionFindItem");
#endif

                                    foreach (string itemId in itemIds)
                                    {
                                        foreach ((string Id, LootItem Item) questItem in questItems)
                                        {
                                            if (questItem.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                                            {
                                                var staticInfo = new QuestData
                                                {
                                                    Id = findItem.id,
                                                    Location = ToQuestLocation(questItem.Item.transform.position),
                                                    NameText = nameKey.Localized(),
                                                    Description = findItem.id.Localized(),
                                                    Trader = TraderIdToName(traderId),
                                                };

                                                questMarkerData.Add(staticInfo);
                                            }
                                        }
                                    }
#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Finished Case ConditionFindItem");
#endif
                                    break;
                                }
                            case ConditionVisitPlace place:
                                {
                                    string zoneId = place.target;

                                    IEnumerable<ExperienceTrigger> zoneTriggers =
                                        allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("ConditionVisitPlace Case Started");
#endif

                                    if (zoneTriggers != null)
                                    {
                                        foreach (ExperienceTrigger trigger in zoneTriggers)
                                        {
                                            var staticInfo = new QuestData
                                            {
                                                Id = place.id,
                                                Location = ToQuestLocation(trigger.transform.position),
                                                ZoneId = zoneId,
                                                NameText = nameKey.Localized(),
                                                Description = place.FormattedDescription.Localized(),
                                                Trader = TraderIdToName(traderId),
                                            };

                                            questMarkerData.Add(staticInfo);
                                        }
                                    }

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("ConditionVisitPlace Case Finished");
#endif
                                    break;
                                }
                            case ConditionCounterCreator counterCreator:
                                {
#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Started Case ConditionCounterCreator");
#endif

                                    var counter = Traverse.Create(counterCreator).Field("counter").GetValue<ConditionCounterCreator>();

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Instantiated counter");
#endif

                                    var conditions = Traverse.Create(counter).Property("Conditions").GetValue<GClass3373>();

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Instantiated conditions");
#endif
                                    var conditionsList = Traverse.Create(conditions).Field("list_0").GetValue<IList>();

#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Instantiated conditionsList");
#endif

                                    if (conditionsList == null)
                                    {
                                        GTFOComponent.Logger.LogInfo("conditionsList is null");
                                        continue;
                                    }

                                    foreach (object condition2 in conditionsList)
                                    {
#if DEBUG
                                        GTFOComponent.Logger.LogInfo("In foreach Loop of ConditionCounterCreator");
#endif

                                        switch (condition2)
                                        {
                                            case ConditionVisitPlace place:
                                                {
                                                    string zoneId = place.target;

                                                    IEnumerable<ExperienceTrigger> zoneTriggers =
                                                        allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

#if DEBUG
                                                    GTFOComponent.Logger.LogInfo("ConditionVisitPlace Case Started");
#endif

                                                    if (zoneTriggers != null)
                                                    {
                                                        foreach (ExperienceTrigger trigger in zoneTriggers)
                                                        {
                                                            var staticInfo = new QuestData
                                                            {
                                                                Id = counterCreator.id,
                                                                Location = ToQuestLocation(trigger.transform.position),
                                                                ZoneId = zoneId,
                                                                NameText = nameKey.Localized(),
                                                                Description = counterCreator.id.Localized(),
                                                                Trader = TraderIdToName(traderId),
                                                            };

                                                            questMarkerData.Add(staticInfo);
                                                        }
                                                    }

#if DEBUG
                                                    GTFOComponent.Logger.LogInfo("ConditionVisitPlace Case Finished");
#endif
                                                    break;
                                                }
                                            case ConditionInZone inZone:
                                                {
                                                    string[] zoneIds = inZone.zoneIds;

#if DEBUG
                                                    GTFOComponent.Logger.LogInfo("ConditionInZone Case Started");
#endif

                                                    foreach (string zoneId in zoneIds)
                                                    {
                                                        IEnumerable<ExperienceTrigger> zoneTriggers =
                                                            allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

                                                        if (zoneTriggers != null)
                                                        {
                                                            foreach (ExperienceTrigger trigger in zoneTriggers)
                                                            {
                                                                var staticInfo = new QuestData
                                                                {
                                                                    Id = counterCreator.id,
                                                                    Location = ToQuestLocation(trigger.transform.position),
                                                                    ZoneId = zoneId,
                                                                    NameText = nameKey.Localized(),
                                                                    Description = counterCreator.id.Localized(),
                                                                    Trader = TraderIdToName(traderId),
                                                                };

                                                                questMarkerData.Add(staticInfo);
                                                            }
                                                        }
                                                    }

#if DEBUG
                                                    GTFOComponent.Logger.LogInfo("ConditionVisitPlace Case Finished");
#endif
                                                    break;
                                                }
                                        }
                                    }
#if DEBUG
                                    GTFOComponent.Logger.LogInfo("Finished Case ConditionCounterCreator");
#endif
                                    break;
                                }
                        }
                    }
                }
            }

            QuestMarkers = questMarkerData;
        }

        internal QuestLocation ToQuestLocation(UnityEngine.Vector3 vector)
        {
            return new QuestLocation(vector.x, vector.y, vector.z);
        }

        internal string TraderIdToName(string traderId)
        {
            if (traderId.Equals("5ac3b934156ae10c4430e83c", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Ragman";
            }

            if (traderId.Equals("54cb50c76803fa8b248b4571", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Prapor";
            }

            if (traderId.Equals("54cb57776803fa99248b456e", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Therapist";
            }

            if (traderId.Equals("579dc571d53a0658a154fbec", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Fence";
            }

            if (traderId.Equals("58330581ace78e27b8b10cee", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Skier";
            }

            if (traderId.Equals("5935c25fb3acc3127c3d8cd9", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Peacekeeper";
            }

            if (traderId.Equals("5a7c2eca46aef81a7ca2145d", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Mechanic";
            }

            if (traderId.Equals("5c0647fdd443bc2504c2d371", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Jaeger";
            }

            if (traderId.Equals("638f541a29ffd1183d187f57", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Prapor";
            }

            if (traderId.Equals("54cb50c76803fa8b248b4571", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Lighthouse Keeper ";
            }

            return traderId.Localized();
        }
    }
}