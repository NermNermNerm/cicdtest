using StardewValley;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public abstract class TractorPartQuest<TQuestState> : BaseQuest<TQuestState> where TQuestState : struct
    {
        protected TractorPartQuest(TractorPartQuestController<TQuestState> controller) : base(controller) { }

        public new TractorPartQuestController<TQuestState> Controller => (TractorPartQuestController<TQuestState>)base.Controller;

        public abstract void GotWorkingPart(Item workingPart);

        public override bool IsItemForThisQuest(Item item)
        {
            return item.ItemId == this.Controller.BrokenAttachmentPartId || item.ItemId == this.Controller.WorkingAttachmentPartId;
        }
    }
}
