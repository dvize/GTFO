using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using static EFT.Quests.ConditionCounterCreator;

namespace GTFO
{
    internal class QuestDataService
    {
        internal List<QuestData> QuestObjectives
        {
            get; set;
        }
        private readonly GameWorld _gameWorld;
        private readonly Player _player;
        public QuestDataService(ref GameWorld gameWorld, ref Player player)
        {
            QuestObjectives = new List<QuestData>();
            _gameWorld = gameWorld ?? throw new ArgumentNullException(nameof(gameWorld));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        internal void InitialQuestData(TriggerWithId[] allTriggers)
        {

            var questObjectiveData = new List<QuestData>();
            var questsList = GetQuestsList();
            var lootItems = GetLootItems();
            (string Id, LootItem Item)[] questItems =
                lootItems.Where(x => x.Item.QuestItem).Select(x => (x.TemplateId, x)).ToArray();

            var trackedQuests = new HashSet<string>();

            if (questsList != null)
            {
                DrawQuestDropdown(questsList);
                foreach (var quest in questsList)
                {
                    if (quest.Status != EQuestStatus.Started)
                        continue;

                    ProcessQuest(quest, allTriggers, questItems, questObjectiveData);
                }
            }

            QuestObjectives = questObjectiveData;
        }

        internal void UpdateQuestCompletedConditions(GClass1249 bsgQuest)
        {
#if DEBUG
            GTFOComponent.Logger.LogWarning($"Updating Completed Quest Data from BSG Quest: {bsgQuest.Id.LocalizedName()} of type {bsgQuest.QuestTypeName} \r ID: {bsgQuest.Id}");
#endif
            // Iterate through QuestObjectives to find the completed objective
            for (int i = 0; i < QuestObjectives.Count; i++)
            {
                if (bsgQuest.CompletedConditions.Contains(QuestObjectives[i].Id))
                {
#if DEBUG
                    GTFOComponent.Logger.LogWarning($"Quest Objective {QuestObjectives[i].Id} is completed and being removed.");
#endif
                    QuestObjectives.RemoveAt(i);

                    break;
                }
            }

        }

        internal void UpdateQuestCompletedConditionals(string id)
        {
            /*#if DEBUG
                        GTFOComponent.Logger.LogWarning($"Updated Special Visit with id: {id}");
            #endif*/
            //find the id in the quest objectives and remove it
            for (int i = 0; i < QuestObjectives.Count; i++)
            {
                /*#if DEBUG
                                GTFOComponent.Logger.LogError($"Checking Zone ID: {QuestObjectives[i].ZoneId}");
                #endif*/
                if (QuestObjectives[i].ZoneId != null && QuestObjectives[i].ZoneId.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    QuestObjectives.RemoveAt(i);
                    break;
                }
            }

        }
        internal List<QuestDataClass> GetQuestsList()
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

        private void ProcessQuest(QuestDataClass quest, TriggerWithId[] allTriggers, (string Id, LootItem Item)[] questItems, List<QuestData> questMarkerData)
        {
            if (quest == null || quest.Template == null || quest.Template.Conditions == null)
            {
                GTFOComponent.Logger.LogError("Invalid quest data");
                return;
            }

            var nameKey = quest?.Template?.NameLocaleKey;
            var traderId = quest?.Template?.TraderId;

            if (nameKey == null || traderId == null)
            {
                GTFOComponent.Logger.LogError("Quest NameLocaleKey or TraderId is missing: " + quest?.Template?.Name);
                return;
            }

#if DEBUG
            GTFOComponent.Logger.LogWarning($"Quest: {nameKey.Localized()}");
            GTFOComponent.Logger.LogWarning($"Description: {quest.Template.Description?.Localized()}");
            GTFOComponent.Logger.LogWarning($"Trader: {traderId.Localized()}");
#endif

            // Ensuring that there is a list of conditions to process
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                GTFOComponent.Logger.LogError("ProcessQuest: 'conditions' is null or not found for AvailableForFinish status");
                return;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    GTFOComponent.Logger.LogError("ProcessQuest: A 'condition' within conditions is null");
                    continue;
                }

#if DEBUG
                GTFOComponent.Logger.LogWarning($"Processing Condition: {condition.id.Localized()}");
#endif

                //check if condition has already been completed
                if (quest.CompletedConditions.Contains(condition.id))
                {
#if DEBUG
                    GTFOComponent.Logger.LogWarning($"Condition {condition.id.Localized()} has already been completed.");
                    continue;
#endif
                }
                ProcessCondition(quest, condition, allTriggers, questItems, nameKey, traderId, questMarkerData);
            }
        }


        private void ProcessCondition(QuestDataClass quest, Condition condition, TriggerWithId[] allTriggers, (string Id, LootItem Item)[] questItems, string nameKey, string traderId, List<QuestData> questMarkerData)
        {
#if DEBUG
            GTFOComponent.Logger.LogInfo("Processing Condition of type: " + condition.GetType());
            GTFOComponent.Logger.LogInfo("\tCondition: " + condition.id.Localized());
#endif

            switch (condition)
            {
                case ConditionLeaveItemAtLocation location:
                    ProcessConditionPlaceItemClass(location.id, location.zoneId, nameKey, traderId, location.IsNecessary, allTriggers, questMarkerData, quest);
                    break;
                case ConditionPlaceBeacon beacon:
                    ProcessConditionPlaceItemClass(beacon.id, beacon.zoneId, nameKey, traderId, beacon.IsNecessary, allTriggers, questMarkerData, quest);
                    break;
                case ConditionFindItem findItem:
                    ProcessFindItemCondition(findItem.id, findItem.target, nameKey, traderId, findItem.IsNecessary, allTriggers, questItems, questMarkerData, quest);
                    break;
                case ConditionLaunchFlare location:
                    ProcessConditionPlaceItemClass(location.id, location.zoneID, nameKey, traderId, location.IsNecessary, allTriggers, questMarkerData, quest);
                    break;
                case ConditionCounterCreator creator:
                    ProcessConditionCounter(creator, nameKey, traderId, allTriggers, questMarkerData, quest);
                    break;
                default:
                    /*#if DEBUG
                                        GTFOComponent.Logger.LogError("Unhandled Condition of type: " + condition.GetType());
                                        GTFOComponent.Logger.LogError("\tCondition: " + condition.id.Localized());
#endif*/
                    break;
            }
        }

        private void ProcessConditionCounter(ConditionCounterCreator counterCreator, string nameKey, string traderId, TriggerWithId[] allTriggers, List<QuestData> questMarkerData, QuestDataClass quest)
        {
            var counter = Traverse.Create(counterCreator).Field("_templateConditions").GetValue<ConditionCounterTemplate>();
            var conditions = Traverse.Create(counter).Field("Conditions").GetValue<GClass3368>();
            var conditionsList = Traverse.Create(conditions).Field("list_0").GetValue<IList<Condition>>();

            foreach (var counterCondition in conditionsList)
            {
#if DEBUG
                GTFOComponent.Logger.LogInfo("\tIn Foreach Loop of ConditionCounterCreator");
                GTFOComponent.Logger.LogInfo("\t\tCounterCondition type: " + counterCondition.GetType());
                GTFOComponent.Logger.LogInfo("\t\tCounterCondition: " + counterCondition.id.Localized());
#endif

                ProcessCounterCondition(counterCondition, nameKey, traderId, allTriggers, questMarkerData, quest);
            }

        }

        private void ProcessConditionPlaceItemClass(string conditionId, string zoneId, string nameKey, string traderId, bool isNecessary,
            IEnumerable<TriggerWithId> allTriggers, List<QuestData> questMarkerData, QuestDataClass quest)
        {
            TriggerWithId[] triggersArray = allTriggers.ToArray();
            IEnumerable<PlaceItemTrigger> zoneTriggers = triggersArray.GetZoneTriggers<PlaceItemTrigger>(zoneId);

            foreach (var trigger in zoneTriggers)
            {
                var questData = new QuestData
                {
                    Id = conditionId,
                    ParentId = null,
                    Location = ToQuestLocation(trigger.transform.position),
                    ZoneId = zoneId,
                    NameText = nameKey.Localized(),
                    Description = conditionId.Localized(),
                    Trader = TraderIdToName(traderId),
                    IsNecessary = isNecessary,
                    IsCompleted = false,
                };

                questMarkerData.Add(questData);
            }
        }

        private void ProcessFindItemCondition(string conditionId, string[] itemIds, string nameKey, string traderId, bool isNecessary, IEnumerable<TriggerWithId> allTriggers,
            (string Id, LootItem Item)[] questItems, List<QuestData> questMarkerData, QuestDataClass quest)
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
                            ParentId = null,
                            Location = ToQuestLocation(questItem.Item.transform.position),
                            NameText = nameKey.Localized(),
                            Description = conditionId.Localized(),
                            Trader = TraderIdToName(traderId),
                            IsNecessary = isNecessary,
                            IsCompleted = false,
                        };

                        questMarkerData.Add(staticInfo);
                    }
                }
            }

        }

        private void ProcessCounterCondition(Condition counterCondition, string nameKey, string traderId, TriggerWithId[] allTriggers, List<QuestData> questMarkerData, QuestDataClass quest)
        {
            switch (counterCondition)
            {
                case ConditionVisitPlace place:
                    ProcessConditionVisitPlace(place, nameKey, traderId, true, allTriggers, questMarkerData, quest);
                    break;
                case ConditionInZone zone:
                    ProcessConditionInZone(zone, nameKey, traderId, true, allTriggers, questMarkerData, quest);
                    break;
                default:
                    /*#if DEBUG
                                        GTFOComponent.Logger.LogError("\tUnhandled Counter Condition of type: " + counterCondition.GetType());
                                        GTFOComponent.Logger.LogError("\tCounter Condition: " + counterCondition.id.Localized());
                    #endif*/
                    break;
            }
        }

        private void ProcessConditionInZone(ConditionInZone zone, string nameKey, string traderId, bool isNecessary, TriggerWithId[] allTriggers, List<QuestData> questMarkerData, QuestDataClass quest)
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
                            ParentId = zone.parentId,
                            Location = ToQuestLocation(trigger.transform.position),
                            ZoneId = zoneId,
                            NameText = nameKey.Localized(),
                            Description = zone.id.Localized(),
                            Trader = TraderIdToName(traderId),
                            IsNecessary = isNecessary,
                            IsCompleted = false,
                        };

#if DEBUG
                        GTFOComponent.Logger.LogInfo("\tSetting isNecessary: " + staticInfo.IsNecessary);
#endif
                        questMarkerData.Add(staticInfo);
                    }
                }
            }

        }

        private void ProcessConditionVisitPlace(ConditionVisitPlace place, string nameKey, string traderId, bool isNecessary, TriggerWithId[] allTriggers, List<QuestData> questMarkerData, QuestDataClass quest)
        {
            string zoneId = place.target;

            IEnumerable<ExperienceTrigger> zoneTriggers =
                allTriggers.GetZoneTriggers<ExperienceTrigger>(zoneId);

            if (zoneTriggers != null && zoneTriggers.Any())
            {
                ExperienceTrigger trigger = zoneTriggers.First();  // Get the first trigger since it spawns nearby triggers anyways?

                var staticInfo = new QuestData
                {
                    Id = place.id,
                    ParentId = quest.Id,
                    Location = ToQuestLocation(trigger.transform.position),
                    ZoneId = zoneId,
                    NameText = nameKey.Localized(),
                    Description = "Visit Location",
                    Trader = TraderIdToName(traderId),
                    IsNecessary = isNecessary,
                    IsCompleted = false,
                };

#if DEBUG
                GTFOComponent.Logger.LogError("\t\tSetting isNecessary: " + staticInfo.IsNecessary + " for quest: " + staticInfo.NameText);
                GTFOComponent.Logger.LogError("\t\tSetting Condition VisitPlace Zone Id: " + staticInfo.ZoneId);
#endif
                questMarkerData.Add(staticInfo);

            }
        }
        internal void Cleanup()
        {
            GTFOComponent.Logger.LogInfo("Cleaning up QuestDataService.");

            // Clear the quest objectives list to release references to quest data
            QuestObjectives = new List<QuestData>();
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

        //create dictionary of location ID Map
        internal Dictionary<string, string> mapnameMapping = new Dictionary<string, string>
        {
            { "any", "any" },
            { "55f2d3fd4bdc2d5f408b4567", "factory4_day" },
            { "59fc81d786f774390775787e", "factory4_night" },
            { "56f40101d2720b2a4d8b45d6", "bigmap" },
            { "5704e3c2d2720bac5b8b4567", "Woods" },
            { "5704e554d2720bac5b8b456e", "Shoreline" },
            { "5714dbc024597771384a510d", "Interchange" },
            { "5704e4dad2720bb55b8b4567", "Lighthouse" },
            { "5b0fc42d86f7744a585f9105", "laboratory" },
            { "5704e5fad2720bc05b8b4567", "RezervBase" },
            { "5714dc692459777137212e12", "TarkovStreets" },
            { "653e6760052c01c1c805532f", "Sandbox" }
        };
        
        internal void DrawQuestDropdown(List<QuestDataClass> questsListOriginal)
        {
            //only if GTFOComponent.questManager.questDataService is not null
            if (GTFOComponent.questManager?.questDataService == null || questsListOriginal == null)
            {
                return;
            }

            var questsList = new List<string>() { "All" };

            // Add available quests to the list
            foreach (var quest in questsListOriginal)
            {
                if(quest.Template != null)
                {
                    if (quest.Status == EQuestStatus.Started &&
                    mapnameMapping[quest?.Template?.LocationId].ToLower() == Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.LocationId.ToLower())
                    {
#if DEBUG
                        GTFOComponent.Logger.LogWarning("DrawQuestDropdown Quest: " + quest.Template.Name + " Status: " + quest.Status);
                        GTFOComponent.Logger.LogWarning("Location: " + mapnameMapping[quest.Template.LocationId].ToLower());
#endif
                        questsList.Add(quest.Template.Name);
                    }
                }
                //if the quest is started and questlocation matches current map
                
            }

            if(questsList.Count > 1)
            {
                GTFOPlugin.Instance.RebindDropDown(questsList);
            }
            

        }


    }
}
