using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace NermNermNerm.Stardew.QuestableTractor
{
    /// <summary>
    ///   This class represents aspects of the class that need monitoring whether or not the player is
    ///   actually on the quest.  For example, if there are triggers that start the quest, the controller
    ///   will detect them.  The controller is also the place that knows whether the quest has been
    ///   completed or not.
    /// </summary>
    /// <remarks>
    ///   Quest Controllers should be constructed when the mod is initialized (in <code>Mod.Entry</code>)
    ///   and they are never destroyed.
    /// </remarks>
    public abstract class BaseQuestController : ISimpleLog
    {
        protected BaseQuestController(ModEntry mod)
        {
            this.Mod = mod;
        }

        protected const string QuestCompleteStateMagicWord = "Complete";

        public ModEntry Mod { get; }

        public abstract void OnDayStarted();
        public abstract void OnDayEnding();

        protected virtual string? ModDataKey { get; } = null;

        public virtual bool IsStarted => Game1.MasterPlayer.modData.ContainsKey(this.ModDataKey);
        public virtual bool IsComplete => Game1.MasterPlayer.modData.TryGetValue(this.ModDataKey, out string value) && value == QuestCompleteStateMagicWord;

        public static void Spout(string message)
        {
            Game1.DrawDialogue(new Dialogue(null, null, message));
        }

        public abstract bool IsItemForThisQuest(Item item);

        /// <summary>
        ///   Conversation keys are managed mod-wide, as there's a complex interplay, where
        ///   the main-quest conversation key is thrown up until that one gets started,
        ///   and then the part quest hints get dribbled out later.
        /// </summary>
        public virtual string? HintTopicConversationKey { get; } = null;

        private readonly Dictionary<string, Action<Item>> itemsToWatch = new();

        private bool isWatchingInventory;

        protected void MonitorInventoryForItem(string itemId, Action<Item> onItemAdded)
        {
            this.itemsToWatch[itemId] = onItemAdded;
            if (!this.isWatchingInventory)
            {
                this.Mod.Helper.Events.Player.InventoryChanged += this.Player_InventoryChanged;
                this.isWatchingInventory = true;
            }
        }

        protected void StopMonitoringInventoryFor(string itemId)
        {
            this.itemsToWatch.Remove(itemId);
            if (!this.itemsToWatch.Any() && this.isWatchingInventory)
            {
                this.Mod.Helper.Events.Player.InventoryChanged -= this.Player_InventoryChanged;
                this.isWatchingInventory = false;
            }
        }


        private void Player_InventoryChanged(object? sender, StardewModdingAPI.Events.InventoryChangedEventArgs e)
        {
            foreach (var item in e.Added)
            {
                if (this.itemsToWatch.TryGetValue(item.ItemId, out var handler))
                {
                    if (!e.Player.IsMainPlayer)
                    {
                        e.Player.holdUpItemThenMessage(item, true);
                        Spout("This item is for unlocking the tractor - only the host can advance this quest.  Give this item to the host.");
                    }
                    else
                    {
                        handler(item);
                    }
                }
            }
        }

        /// <summary>
        ///   This is a hacky way to deal with quest completion until something more clever can be thought up.
        ///   Right now this gets called in the 1-second-tick callback.  It returns true if the item resulted
        ///   in quest completion and the tractor config should be rebuilt.
        /// </summary>
        public virtual bool PlayerIsInGarage(Item itemInHand) { return false; }

        public virtual void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => ((ISimpleLog)this.Mod).WriteToLog(message, level, isOnceOnly);
    }


    public abstract class BaseQuestController<TStateEnum, TQuest> : BaseQuestController
        where TStateEnum : struct, Enum
        where TQuest : BaseQuest<TStateEnum>
    {
        protected BaseQuestController(ModEntry mod) : base(mod) { }

        protected virtual TQuest CreateQuestFromDeserializedState(TStateEnum initialState)
        {
            throw new Exception("Implementations of BaseQuestController must override either CreateQuestFromDeserializedState and ModDataKey or Deserialize");
        }

        protected abstract TQuest CreateQuest();

        public TQuest? GetQuest() => Game1.player.questLog.OfType<TQuest>().FirstOrDefault();

        protected abstract string QuestCompleteMessage { get; }

        protected enum QuestState
        {
            NotStarted,
            InProgress,
            Completed,
        };

        protected virtual QuestState Deserialize(out TQuest? quest)
        {
            if (this.ModDataKey is null)
            {
                throw new Exception("Subclasses of BaseQuestController should either set ModDataKey or override Deserialize");
            }

            if (!Game1.player.modData.TryGetValue(this.ModDataKey, out string storedValue))
            {
                quest = null;
                return QuestState.NotStarted;
            }

            if (storedValue == QuestCompleteStateMagicWord)
            {
                quest = null;
                return QuestState.Completed;
            }

            return this.DeserializeSingleKey(storedValue, out quest);
        }

        protected virtual QuestState DeserializeSingleKey(string storedValue, out TQuest? quest)
        {
            if (!Enum.TryParse(storedValue, out TStateEnum parsedValue))
            {
                this.LogError($"Invalid value for moddata key, '{this.ModDataKey}': '{storedValue}' - quest state will revert to not started.");
                quest = null;
                return QuestState.Completed;
            }

            quest = this.CreateQuestFromDeserializedState(parsedValue);
            return QuestState.InProgress;
        }

        protected virtual void OnDayStartedQuestNotStarted()
        {
            if (this.HintTopicConversationKey is not null)
            {
                // Our stuff recurs every week for 4 days out of the week.  Delay until after the
                // first week so that the introductions quest runs to completion.  Perhaps it
                // would be better to delay until all the villagers we care about have been greeted.
                if (Game1.Date.DayOfWeek == DayOfWeek.Sunday)
                {
                    // TODO: Drop hint
                }
            }

            // 
            // implementations should hide the quest starter
        }

        protected virtual void OnDayStartedQuestInProgress(TQuest quest)
        {
            quest.AdvanceStateForDayPassing();
        }

        protected virtual void OnDayStartedQuestComplete()
        {
        }

        public sealed override void OnDayStarted()
        {
            var state = this.Deserialize(out var newQuest);
            if (newQuest is not null)
            {
                newQuest.MarkAsViewed();
                newQuest.MakeSoundOnAdvancement = true;
                Game1.player.questLog.Add(newQuest);
            }
            else if (state == QuestState.NotStarted)
            {
                this.OnDayStartedQuestNotStarted();
            }
            else
            {
                this.OnDayStartedQuestComplete();
            }
        }

        /// <summary>
        ///  Called once a day when the quest is active to ensure that we're monitoring for items even after reload
        /// </summary>
        protected virtual void MonitorQuestItems() { }

        public override void OnDayEnding()
        {
            var quest = Game1.player.questLog.OfType<TQuest>().FirstOrDefault();
            if (quest is not null)
            {
                this.SaveQuestAtEndOfDay(quest);
                Game1.player.questLog.RemoveWhere(q => q is TQuest);
            }
        }

        public virtual void SaveQuestAtEndOfDay(TQuest quest)
        {
            if (this.ModDataKey is null)
            {
                throw new Exception("If ModDataKey is not supplied, SaveQuestAtEndOfDay should be overridden");
            }

            Game1.player.modData[this.ModDataKey] = quest.Serialize();
        }
    }
}
