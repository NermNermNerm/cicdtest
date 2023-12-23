using System;
using StardewModdingAPI;
using StardewValley.TerrainFeatures;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public class ScytheQuestController : TractorPartQuestController<ScytheQuestState>
    {
        public ScytheQuestController(ModEntry mod) : base(mod) { }

        protected override ScytheQuest CreatePartQuest() => new ScytheQuest(this);

        public override string WorkingAttachmentPartId => ObjectIds.WorkingScythe;
        public override string BrokenAttachmentPartId => ObjectIds.BustedScythe;
        public override string HintTopicConversationKey => ConversationKeys.ScytheNotFound;
        protected override string QuestCompleteMessage => "Sweet!  You've now got a harvester attachment for your tractor!#$b#HINT: To use it, equip the scythe while on the tractor.";
        protected override string ModDataKey => ModDataKeys.ScytheQuestStatus;
        protected override void HideStarterItemIfNeeded() => base.PlaceBrokenPartUnderClump(ResourceClump.hollowLogIndex);


        protected override ScytheQuestState AdvanceStateForDayPassing(ScytheQuestState oldState) => oldState;

        public new ScytheQuestState State
        {
            get
            {
                string? rawState = this.RawQuestState;
                if (rawState == null)
                {
                    throw new InvalidOperationException("State should not be queried when the quest isn't started");
                }

                if (!ScytheQuestState.TryParse(rawState, out var result))
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

    }
}
