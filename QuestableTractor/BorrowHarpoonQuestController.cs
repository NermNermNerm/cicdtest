using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public class BorrowHarpoonQuestController
        : BaseQuestController<BorrowHarpoonQuestState>
    {
        public BorrowHarpoonQuestController(ModEntry entry) : base(entry) { }

        protected override string ModDataKey => ModDataKeys.BorrowHarpoonQuestStatus;

        public override bool IsItemForThisQuest(Item item) => item.ItemId == WatererQuestController.HarpoonToolId;

        protected override BorrowHarpoonQuestState AdvanceStateForDayPassing(BorrowHarpoonQuestState oldState) => oldState;

        protected override BaseQuest CreateQuest()
        {
            return new BorrowHarpoonQuest(this);
        }


        public void StartQuest()
        {
            if (!this.IsStarted)
            {
                Game1.addHUDMessage(new HUDMessage("Whoah, I snagged onto something big down there, but this line's nowhere near strong enough to yank it up!", HUDMessage.newQuest_type));
                this.CreateQuestNew();
                Game1.playSound("questcomplete"); // Note documentation suggests its for quest complete and "journal update".  That's what we are using it for.
            }
        }

    }
}
