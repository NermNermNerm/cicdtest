using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using StardewValley.Quests;

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

        protected abstract string ModDataKey { get; }

        public bool IsStarted => this.OverallQuestState != OverallQuestState.NotStarted;
        public bool IsComplete => this.OverallQuestState == OverallQuestState.Completed;

        public static void Spout(string message)
        {
            Game1.DrawDialogue(new Dialogue(null, null, message));
        }

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

        public string? RawQuestState
        {
            get
            {
                Game1.player.modData.TryGetValue(this.ModDataKey, out string storedValue);
                return storedValue;
            }
            set
            {
                if (!Game1.player.IsMainPlayer)
                {
                    throw new NotImplementedException("QuestableTractorMod quests should only be playable by the main player");
                }

                if (value is null)
                {
                    Game1.player.modData.Remove(this.ModDataKey);
                }
                else
                {
                    Game1.player.modData[this.ModDataKey] = value;
                }
            }
        }

        public OverallQuestState OverallQuestState =>
            this.RawQuestState switch
            {
                null => OverallQuestState.NotStarted,
                QuestCompleteStateMagicWord => OverallQuestState.Completed,
                _ => OverallQuestState.InProgress
            };


        /// <summary>
        ///   This is a hacky way to deal with quest completion until something more clever can be thought up.
        ///   Right now this gets called in the 1-second-tick callback.  It returns true if the item resulted
        ///   in quest completion and the tractor config should be rebuilt.
        /// </summary>
        public virtual bool PlayerIsInGarage(Item itemInHand) { return false; }

        public virtual void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => ((ISimpleLog)this.Mod).WriteToLog(message, level, isOnceOnly);

        /// <summary>
        ///   Creates a new instance of the Quest object, assuming the State is correct.
        /// </summary>
        /// <remarks>
        ///   Perhaps it should also take the role of ensuring that the state is actually valid
        ///   and correcting it if not.
        /// </remarks>
        protected abstract BaseQuest CreateQuest();

        protected abstract string InitialQuestState { get; }

        /// <summary>
        ///   Creates a new instance of the Quest object, assuming the State empty.
        /// </summary>
        public void CreateQuestNew()
        {
            this.RawQuestState = this.InitialQuestState;
            var quest = this.CreateQuest();
            quest.SetDisplayAsNew();
            Game1.player.questLog.Add(quest);
            this.MonitorQuestItems();
        }

        public BaseQuest? GetQuest() => Game1.player.questLog.OfType<BaseQuest>().FirstOrDefault(bc => bc.Controller == this);

        protected virtual void OnDayStartedQuestNotStarted()
        {
            // implementations should hide the quest starter
        }

        protected abstract void OnDayStartedQuestInProgress();

        protected virtual void OnDayStartedQuestComplete()
        {
        }

        public void OnDayStarted()
        {
            switch (this.OverallQuestState)
            {
                case OverallQuestState.NotStarted:
                    this.OnDayStartedQuestNotStarted();
                    break;
                case OverallQuestState.InProgress:
                    this.OnDayStartedQuestInProgress();
                    var newQuest = this.CreateQuest();
                    newQuest.MarkAsViewed();
                    Game1.player.questLog.Add(newQuest);
                    this.MonitorQuestItems();
                    break;
                case OverallQuestState.Completed:
                    this.OnDayStartedQuestComplete();
                    break;
            }
        }

        /// <summary>
        ///  Called once a day when the quest is active to ensure that we're monitoring for items even after reload
        /// </summary>
        protected virtual void MonitorQuestItems() { }

        public void OnDayEnding()
        {
            Game1.player.questLog.RemoveWhere(q => q is BaseQuest bq && bq.Controller == this);
        }
    }

    public abstract class BaseQuestController<TQuestState>
        : BaseQuestController
        where TQuestState : struct
    {
        public BaseQuestController(ModEntry mod) : base(mod) { }

        protected override string InitialQuestState => default(TQuestState).ToString()!;

        public TQuestState State
        {
            get
            {
                string? rawState = this.RawQuestState;
                if (rawState == null)
                {
                    throw new InvalidOperationException("State should not be queried when the quest isn't started");
                }

                if (!this.TryParse(rawState, out TQuestState result))
                {
                    // Part of the design of the state enums should include making sure that the default value of
                    // the enum is the starting condition of the quest, so we can possibly recover from this error.
                    this.LogError($"{this.GetType().Name} quest has invalid state: {rawState}");
                }

                return result;
            }
            set
            {
                this.RawQuestState = value.ToString();
            }
        }

        protected virtual bool TryParse(string rawState, out TQuestState result) => Enum.TryParse(rawState, out result);

        protected override void OnDayStartedQuestInProgress()
        {
            this.State = this.AdvanceStateForDayPassing(this.State);
        }

        protected abstract TQuestState AdvanceStateForDayPassing(TQuestState oldState);
    }
}
