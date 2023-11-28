using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace NermNermNerm.Stardew.QuestableTractor
{
    internal class LoaderQuestController
        : BaseQuestController<LoaderQuestState, LoaderQuest>
    {
        public LoaderQuestController(ModEntry mod)
            : base(mod)
        {
            // TODO: If the quest isn't complete, register an inventory listener, looking for old and new shoes.
        }

        protected override string QuestCompleteMessage => "Sweet!  You've now got a front-end loader attachment for your tractor to clear out debris!#$b#HINT: To use it, equip the pick or the axe while on the tractor.";
        protected override string ModDataKey => ModDataKeys.LoaderQuestStatus;
        public override string WorkingAttachmentPartId => ObjectIds.WorkingLoader;
        public override string BrokenAttachmentPartId => ObjectIds.BustedLoader;
        public override string HintTopicConversationKey => ConversationKeys.LoaderNotFound;

        protected override void OnQuestStarted()
        {
            this.MonitorQuestItems();
            base.OnQuestStarted();
        }

        protected override void MonitorQuestItems()
        {
            this.MonitorInventoryForItem(ObjectIds.AlexesOldShoe, this.OnPlayerGotOldShoe);
            this.MonitorInventoryForItem(ObjectIds.DisguisedShoe, this.OnPlayerGotDisguisedShoe);
        }

        private void OnPlayerGotOldShoe(Item oldShoes)
        {
            this.StopMonitoringInventoryFor(ObjectIds.AlexesOldShoe);
            var quest = Game1.player.questLog.OfType<LoaderQuest>().FirstOrDefault();
            if (quest is null)
            {
                this.mod.Monitor.Log($"Player found {oldShoes.ItemId} when the Loader quest was not active?!", LogLevel.Warn);
                return;
            }
            Game1.player.holdUpItemThenMessage(oldShoes);
            if (quest.State < LoaderQuestState.DisguiseTheShoes)
            {
                quest.State = LoaderQuestState.DisguiseTheShoes;
            }

            if (Game1.player.currentLocation.Name == "Mine")
            {
                // crazy long duration since the player could take a while getting hold of the language scrolls.
                // Note that if the player talks to the dwarf, it'll probably eat this event anyway.  Such is life.
                Game1.player.activeDialogueEvents.Add(ConversationKeys.DwarfShoesTaken, 100);
            }

            LoaderQuest.RemoveShoesNearDwarf();
        }

        private void OnPlayerGotDisguisedShoe(Item dyedShoes)
        {
            this.StopMonitoringInventoryFor(ObjectIds.DisguisedShoe);
            var quest = Game1.player.questLog.OfType<LoaderQuest>().FirstOrDefault();
            if (quest is null)
            {
                this.mod.Monitor.Log($"Player found {dyedShoes.ItemId}, when the quest was not active?!", LogLevel.Warn);
                return;
            }
            Game1.player.holdUpItemThenMessage(dyedShoes);
            if (quest.State < LoaderQuestState.GiveShoesToClint)
            {
                quest.State = LoaderQuestState.GiveShoesToClint;
            }
        }

        protected override void HideStarterItemIfNeeded()
        {
            this.PlaceBrokenPartUnderClump(ResourceClump.boulderIndex);
        }

        // Shenanigans to do...
        // Plant the shoes, but only if the quest is active and has reached the stage where we know we want shoes.
        // Know if the player has gotten alex's old shoes
        // register for trashcan stuff
        //   Looks like you can add items, and maybe even on-demand, from Data\\GarbageCans of type GarbageCanData where the id of the
        //   mullner's can is either "evelyn" or "6"
    }
}
