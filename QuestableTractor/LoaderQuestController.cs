using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.GarbageCans;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace NermNermNerm.Stardew.QuestableTractor
{
    internal class LoaderQuestController
        : BaseQuestController<LoaderQuestState, LoaderQuest>
    {
        public LoaderQuestController(ModEntry mod) : base(mod) { }

        protected override string QuestCompleteMessage => "Sweet!  You've now got a front-end loader attachment for your tractor to clear out debris!#$b#HINT: To use it, equip the pick or the axe while on the tractor.";
        protected override string ModDataKey => ModDataKeys.LoaderQuestStatus;
        public override string WorkingAttachmentPartId => ObjectIds.WorkingLoader;
        public override string BrokenAttachmentPartId => ObjectIds.BustedLoader;
        public override string HintTopicConversationKey => ConversationKeys.LoaderNotFound;

        protected override LoaderQuest CreateQuest() => new LoaderQuest();

        protected override LoaderQuest CreateQuestFromDeserializedState(LoaderQuestState initialState) => new LoaderQuest(initialState);

        protected override void OnQuestStarted()
        {
            this.MonitorQuestItems();
            base.OnQuestStarted();
        }

        protected override void MonitorQuestItems()
        {
            this.MonitorInventoryForItem(ObjectIds.AlexesOldShoe, this.OnPlayerGotOldShoes);
            this.MonitorInventoryForItem(ObjectIds.DisguisedShoe, this.OnPlayerGotDisguisedShoes);
        }

        private void OnPlayerGotOldShoes(Item oldShoes)
        {
            this.StopMonitoringInventoryFor(ObjectIds.AlexesOldShoe);
            var quest = Game1.player.questLog.OfType<LoaderQuest>().FirstOrDefault();
            if (quest is null)
            {
                this.Mod.Monitor.Log($"Player found {oldShoes.ItemId} when the Loader quest was not active?!", LogLevel.Warn);
            }
            else
            {
                quest.OnPlayerGotOldShoes(oldShoes);
            }
        }

        private void OnPlayerGotDisguisedShoes(Item dyedShoes)
        {
            this.StopMonitoringInventoryFor(ObjectIds.DisguisedShoe);
            var quest = this.GetQuest();
            if (quest is null)
            {
                this.Mod.Monitor.Log($"Player found {dyedShoes.ItemId}, when the quest was not active?!", LogLevel.Warn);
            }
            else
            {
                quest.OnPlayerGotDisguisedShoes(dyedShoes);
            }
        }

        protected override void HideStarterItemIfNeeded()
        {
            this.PlaceBrokenPartUnderClump(ResourceClump.boulderIndex);
        }

        internal void EditGarbageCanAsset(GarbageCanData gcd)
        {
            this.GetQuest()?.EditGarbageCanAsset(gcd);
        }
     }
}
