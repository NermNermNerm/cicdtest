using StardewValley;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public abstract class TractorPartQuest<TQuestState> : BaseQuest<TQuestState> where TQuestState : struct
    {
        protected TractorPartQuest(TractorPartQuestController<TQuestState> controller) : base(controller) { }

        public abstract void GotWorkingPart(Item workingPart);
    }
}
