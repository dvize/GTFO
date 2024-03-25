using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Quests;
using HarmonyLib;
using UnityEngine;
using static EFT.Quests.ConditionCounterCreator;

namespace GTFO
{
    internal class QuestDataService
    {
        public IReadOnlyList<QuestData> QuestMarkers { get; private set; } = Array.Empty<QuestData>();
        private readonly GameWorld _gameWorld;
        private readonly Player _player;

        // Constructor for dependency injection
        public QuestDataService(GameWorld gameWorld, Player player)
        {
            _gameWorld = gameWorld ?? throw new ArgumentNullException(nameof(gameWorld));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void ReloadQuestData(TriggerWithId[] allTriggers)
        {
            var questMarkerData = new List<QuestData>();
            var questsList = GetQuestsList();
            var lootItems = GetLootItems();
            (string Id, LootItem Item)[] questItems =
                lootItems.Where(x => x.Item.QuestItem).Select(x => (x.TemplateId, x)).ToArray();

            if (questsList != null)
            {
                foreach (var quest in questsList)
                {
                    if (quest.Status != EQuestStatus.Started)
                        continue;

                    ProcessQuest(quest, allTriggers, questItems, ref questMarkerData);
                }
            }

            QuestMarkers = questMarkerData;
        }

        private List<QuestDataClass> GetQuestsList()
        {
            var absQuestController = Traverse.Create(_player).Field("_questController").GetValue<AbstractQuestControllerClass>();
            var quests = Traverse.Create(absQuestController).Property("Quests").GetValue<GClass3362>();
            var questsList = Traverse.Create(quests).Field("list_1").GetValue<List<QuestDataClass>>();

            return questsList;
        }

        private List<LootItem> GetLootItems()
        {
            var lootItemsList = Traverse.Create(_gameWorld).Field("LootItems").Field("list_0").GetValue<List<LootItem>>();

            return lootItemsList;
        }

        private void ProcessQuest(QuestDataClass quest, TriggerWithId[] allTriggers, (string Id, LootItem Item)[] questItems, ref List<QuestData> questMarkerData)
        {
            var nameKey = quest.Template.NameLocaleKey;
            var traderId = quest.Template.TraderId;

            foreach (var condition in quest.Template.Conditions[EQuestStatus.AvailableForFinish])
            {
                ProcessCondition(condition, allTriggers, questItems, nameKey, traderId, ref questMarkerData);
            }
        }

        private void ProcessCondition(Condition condition, TriggerWithId[] allTriggers,(string Id, LootItem Item)[] questItems, string nameKey, string traderId, ref List<QuestData> questMarkerData)
        {
#if DEBUG
            GTFOComponent.Logger.LogInfo("Processing Condition of type: " + condition.GetType());
            GTFOComponent.Logger.LogInfo("Condition: " + condition.id.Localized());
#endif
            switch (condition)
            {
                case ConditionLeaveItemAtLocation location:
                    ProcessConditionGeneric(location.id, location.zoneId, nameKey, traderId, location.IsNecessary, allTriggers, ref questMarkerData);
                    break;
                case ConditionPlaceBeacon beacon:
                    ProcessConditionGeneric(beacon.id, beacon.zoneId, nameKey, traderId, beacon.IsNecessary, allTriggers, ref questMarkerData);
                    break;
                case ConditionFindItem findItem:
                    ProcessFindItemCondition(findItem.id, findItem.target, nameKey, traderId, findItem.IsNecessary, allTriggers, questItems, ref questMarkerData);
                    break;
                case ConditionLaunchFlare location:
                    ProcessConditionGeneric(location.id, location.zoneID, nameKey, traderId, location.IsNecessary, allTriggers, ref questMarkerData);
                    break;
                case ConditionCounterCreator creator:
                    ProcessConditionCounter(creator, nameKey, traderId, creator.IsNecessary, allTriggers, ref questMarkerData);
                    break;
                default:
#if DEBUG
                    GTFOComponent.Logger.LogError("Unhandled Condition of type: " + condition.GetType());
                    GTFOComponent.Logger.LogError("Condition: " + condition.id.Localized());
#endif
                    break;
            }
        }

        private void ProcessConditionCounter(ConditionCounterCreator counterCreator, string nameKey, string traderId, bool isNecessary, TriggerWithId[] allTriggers, ref List<QuestData> questMarkerData)
        {
            var counter = Traverse.Create(counterCreator).Field("_templateConditions").GetValue<ConditionCounterTemplate>();
            var conditions = Traverse.Create(counter).Field("Conditions").GetValue<GClass3368>();
            var conditionsList = Traverse.Create(conditions).Field("list_0").GetValue<IList<Condition>>();

            foreach (var counterCondition in conditionsList)
            {
#if DEBUG
                GTFOComponent.Logger.LogInfo("In Foreach Loop of ConditionCounterCreator");
                GTFOComponent.Logger.LogInfo("CounterCondition type: " + counterCondition.GetType());
                GTFOComponent.Logger.LogInfo("CounterCondition: " + counterCondition.id.Localized());
#endif

                ProcessCounterCondition(counterCondition, nameKey, traderId, isNecessary, allTriggers, ref questMarkerData);
            }

        }

        private void ProcessConditionGeneric(string conditionId, string zoneId, string nameKey, string traderId, bool isNecessary, IEnumerable<TriggerWithId> allTriggers, ref List<QuestData> questMarkerData)
        {
            TriggerWithId[] triggersArray = allTriggers.ToArray();
            IEnumerable<PlaceItemTrigger> zoneTriggers = triggersArray.GetZoneTriggers<PlaceItemTrigger>(zoneId);

            foreach (var trigger in zoneTriggers)
            {
                var questData = new QuestData
                {
                    Id = conditionId,
                    Location = ToQuestLocation(trigger.transform.position),
                    ZoneId = zoneId,
                    NameText = nameKey.Localized(),
                    Description = conditionId.Localized(),
                    Trader = TraderIdToName(traderId),
                    IsNecessary = isNecessary,
                };

                questMarkerData.Add(questData);
            }
        }

        private void ProcessFindItemCondition(string conditionId, string[] itemIds, string nameKey, string traderId, bool isNecessary, IEnumerable<TriggerWithId> allTriggers, (string Id, LootItem Item)[] questItems, ref List<QuestData> questMarkerData)
        {
            foreach (string itemId in itemIds)
            {
                foreach ((string Id, LootItem Item) questItem in questItems)
                {
                    if (questItem.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        var staticInfo = new QuestData
                        {
                            Id = conditionId,
                            Location = ToQuestLocation(questItem.Item.transform.position),
                            NameText = nameKey.Localized(),
                            Description = conditionId.Localized(),
                            Trader = TraderIdToName(traderId),
                            IsNecessary = isNecessary,
                        };

                        questMarkerData.Add(staticInfo);
                    }
                }
            }

        }

        private void ProcessCounterCondition(Condition counterCondition, string nameKey, string traderId, bool isNecessary, TriggerWithId[] allTriggers, ref List<QuestData> questMarkerData)
        {
            switch (counterCondition)
            {
                case ConditionVisitPlace place:
                    ProcessConditionGeneric(place.id, place.target, nameKey, traderId, place.IsNecessary, allTriggers, ref questMarkerData);
                    break;
                case ConditionInZone zone:
                    ProcessConditionInZone(zone, nameKey, traderId, zone.IsNecessary, allTriggers, ref questMarkerData);
                    break;
                default:
#if DEBUG
                    GTFOComponent.Logger.LogError("Unhandled Counter Condition of type: " + counterCondition.GetType());
                    GTFOComponent.Logger.LogError("Counter Condition: " + counterCondition.id.Localized());
#endif
                    break;
            }
        }

        private void ProcessConditionInZone(ConditionInZone zone, string nameKey, string traderId, bool isNecessary, TriggerWithId[] allTriggers, ref List<QuestData> questMarkerData)
        {
            string[] zoneIds = zone.zoneIds;

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
                            Id = zone.id,
                            Location = ToQuestLocation(trigger.transform.position),
                            ZoneId = zoneId,
                            NameText = nameKey.Localized(),
                            Description = zone.id.Localized(),
                            Trader = TraderIdToName(traderId),
                            IsNecessary = isNecessary,
                        };

                        questMarkerData.Add(staticInfo);
                    }
                }
            }

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
