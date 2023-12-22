using StardewValley;

namespace NermNermNerm.Stardew.QuestableTractor
{
    internal class SeederQuestController
        : TractorPartQuestController<SeederQuestState, SeederQuest>
    {
        public SeederQuestController(ModEntry mod) : base(mod) { }

        protected override SeederQuest CreateQuest() => new SeederQuest();

        protected override SeederQuest CreateQuestFromDeserializedState(SeederQuestState initialState)
            => new SeederQuest(initialState);

        protected override string QuestCompleteMessage => "Awesome!  You've now got a way to plant and fertilize crops with your tractor!#$b#HINT: To use it, equip seeds or fertilizers while on the tractor.";

        protected override string ModDataKey => ModDataKeys.SeederQuestStatus;

        public override string WorkingAttachmentPartId => ObjectIds.WorkingSeeder;

        public override string BrokenAttachmentPartId => ObjectIds.BustedSeeder;

        public override string HintTopicConversationKey => ConversationKeys.SeederNotFound;

        protected override void HideStarterItemIfNeeded()
        {
            if (Game1.player.getFriendshipHeartLevelForNPC("George") >= SeederQuest.GeorgeSendsBrokenPartHeartLevel
                && RestoreTractorQuest.IsTractorUnlocked
                && !Game1.player.modData.ContainsKey(ModDataKeys.SeederQuestGeorgeSentMail))
            {
                Game1.player.mailbox.Add(MailKeys.GeorgeSeederMail);
                Game1.player.modData[ModDataKeys.SeederQuestGeorgeSentMail] = "sent";
            }
        }
    }
}
