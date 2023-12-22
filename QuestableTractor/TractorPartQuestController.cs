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
    public abstract class TractorPartQuestController<TStateEnum, TQuest> : BaseQuestController<TStateEnum, TQuest>
        where TStateEnum : struct, Enum
        where TQuest : BaseQuest<TStateEnum>
    {
        protected TractorPartQuestController(ModEntry mod) : base(mod) { }

        public abstract string WorkingAttachmentPartId { get; }
        public abstract string BrokenAttachmentPartId { get; }

        public virtual void AnnounceGotBrokenPart(Item brokenPart)
        {
            Game1.player.holdUpItemThenMessage(brokenPart);
        }

        public override bool IsItemForThisQuest(Item item) => item.ItemId == this.BrokenAttachmentPartId;

        /// <summary>
        ///  Called when the main player gets the starter item for the quest.  Implmenetations should disable spawning any more quest starter items.
        /// </summary>
        protected virtual void OnQuestStarted() { }

        protected sealed override void OnDayStartedQuestNotStarted()
        {
            this.MonitorInventoryForItem(this.BrokenAttachmentPartId, this.PlayerGotBrokenPart);
            this.HideStarterItemIfNeeded();
        }

        protected sealed override void OnDayStartedQuestInProgress(TQuest quest)
        {
            this.MonitorInventoryForItem(this.WorkingAttachmentPartId, this.PlayerGotWorkingPart);
        }

        private void PlayerGotBrokenPart(Item brokenPart)
        {
            if (Game1.player.questLog.OfType<TQuest>().Any())
            {
                this.LogWarning($"Player found a broken attachment, {brokenPart.ItemId}, when the quest was active?!");
                return;
            }

            this.AnnounceGotBrokenPart(brokenPart);
            var quest = this.CreateQuest();
            Game1.player.questLog.Add(quest);
            this.OnQuestStarted();
            this.MonitorInventoryForItem(this.WorkingAttachmentPartId, this.PlayerGotWorkingPart);
            this.StopMonitoringInventoryFor(this.BrokenAttachmentPartId);
        }

        public void PlayerGotWorkingPart(Item workingPart)
        {
            var quest = Game1.player.questLog.OfType<TQuest>().FirstOrDefault();
            if (quest is null)
            {
                this.LogWarning($"Player found a working attachment, {workingPart.ItemId}, when the quest was not active?!");
                // consider recovering by creating the quest?
                return;
            }

            quest.GotWorkingPart(workingPart);
            this.StopMonitoringInventoryFor(this.WorkingAttachmentPartId);
        }

        public override bool PlayerIsInGarage(Item itemInHand)
        {
            if (itemInHand.ItemId != this.WorkingAttachmentPartId)
            {
                return false;
            }

            var activeQuest = this.GetQuest();
            if (activeQuest is null)
            {
                this.LogWarning($"An active {nameof(TQuest)} should exist, but doesn't?!");
                return false;
            }

            activeQuest?.questComplete();
            Game1.player.modData[this.ModDataKey] = QuestCompleteStateMagicWord;
            Game1.player.removeFirstOfThisItemFromInventory(this.WorkingAttachmentPartId);
            Game1.DrawDialogue(new Dialogue(null, null, this.QuestCompleteMessage));
            return true;
        }

        protected virtual void HideStarterItemIfNeeded() { }

        protected virtual TQuest? Deserialize(string storedValue)
        {
            if (!Enum.TryParse(storedValue, out TStateEnum parsedValue))
            {
                this.LogError($"Invalid value for moddata key, '{this.ModDataKey}': '{storedValue}' - quest state will revert to not started.");
                return null;
            }
            else
            {
                return this.CreateQuestFromDeserializedState(parsedValue);
            }
        }

        protected void PlaceBrokenPartUnderClump(int preferredResourceClumpToHideUnder)
        {
            var farm = Game1.getFarm();
            var existing = farm.objects.Values.FirstOrDefault(o => o.ItemId == this.BrokenAttachmentPartId);
            if (existing is not null)
            {
                this.Mod.Monitor.VerboseLog($"{this.BrokenAttachmentPartId} is already placed at {existing.TileLocation.X},{existing.TileLocation.Y}");
                return;
            }

            var position = this.FindPlaceToPutItem(preferredResourceClumpToHideUnder);
            if (position != default)
            {
                var o = ItemRegistry.Create<StardewValley.Object>(this.BrokenAttachmentPartId);
                o.Location = Game1.getFarm();
                o.TileLocation = position;
                this.Mod.Monitor.VerboseLog($"{this.BrokenAttachmentPartId} placed at {position.X},{position.Y}");
                o.IsSpawnedObject = true;
                farm.objects[o.TileLocation] = o;
            }
        }

        private Vector2 FindPlaceToPutItem(int preferredResourceClumpToHideUnder)
        {
            var farm = Game1.getFarm();
            var bottomMostResourceClump = farm.resourceClumps.Where(tf => tf.parentSheetIndex.Value == preferredResourceClumpToHideUnder).OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
            if (bottomMostResourceClump is not null)
            {
                return bottomMostResourceClump.Tile;
            }

            this.LogWarning($"Couldn't find the preferred location ({preferredResourceClumpToHideUnder}) for the {this.BrokenAttachmentPartId}");
            bottomMostResourceClump = farm.resourceClumps.OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
            if (bottomMostResourceClump is not null)
            {
                return bottomMostResourceClump.Tile;
            }

            this.LogWarning($"The farm contains no resource clumps under which to stick the {this.BrokenAttachmentPartId}");

            // We're probably dealing with an old save,  Try looking for any clear space.
            //  This technique is kinda dumb, but whatev's.  This mod is pointless on a fully-developed farm.
            for (int i = 0; i < 1000; ++i)
            {
                Vector2 positionToCheck = new Vector2(Game1.random.Next(farm.map.DisplayWidth / 64), Game1.random.Next(farm.map.DisplayHeight / 64));
                if (farm.CanItemBePlacedHere(positionToCheck))
                {
                    return positionToCheck;
                }
            }

            this.LogError($"Couldn't find any place at all to put the {this.BrokenAttachmentPartId}");
            return default;
        }
    }
}
