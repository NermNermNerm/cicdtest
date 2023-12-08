using System;
using System.Linq;
using StardewValley;
using StardewValley.Quests;

namespace NermNermNerm.Stardew.QuestableTractor
{

    internal class BorrowHarpoonQuest
        : BaseQuest<BorrowHarpoonQuest.HarpoonQuestState>
    {
        internal enum HarpoonQuestState
        {
            GetThePole,
            CatchTheBigOne,
            ReturnThePole
        }

        public BorrowHarpoonQuest()
            : this(HarpoonQuestState.GetThePole)
        {
            this.ShouldDisplayAsNew();
        }

        private BorrowHarpoonQuest(HarpoonQuestState state)
            : base(state)
        {
            this.questTitle = "We need a bigger pole";
            this.questDescription = "There's something big down at the bottom of the farm pond.  Maybe Willy can loan me something to help get it out.";
        }

        protected override void SetObjective()
        {
            switch (this.State)
            {
                case HarpoonQuestState.GetThePole:
                    this.currentObjective = "Go find Willy in his shop";
                    break;
                case HarpoonQuestState.CatchTheBigOne:
                    this.currentObjective = "Use Willy's harpoon to haul in whatever's at the bottom of the pond";
                    break;
                case HarpoonQuestState.ReturnThePole:
                    this.currentObjective = "Give the Harpoon back to Willy";
                    break;
            }
        }

        public void LandedTheWaterer() => this.State = HarpoonQuestState.ReturnThePole;

        public override void GotWorkingPart(Item workingPart)
        {
            // Shouldn't happen.  There are a couple of QuestBase things that only work for part quests.
        }

        protected override bool IsItemForThisQuest(Item item)
        {
            // This doesn't really work, as CheckIfComplete only gets an item if it's a regular item, not a tool.
            //  But if we don't do an override, the base implementation fails because we don't have a Controller.
            return item.ItemId == WatererQuestController.HarpoonToolId;
        }

        public override void CheckIfComplete(NPC n, Item? item)
        {
            if (n.Name == "Willy" && this.State == HarpoonQuestState.ReturnThePole && this.TryTakeItemsFromPlayer(WatererQuestController.HarpoonToolId))
            {
                this.Spout(n, "Ya reeled that ol water tank on wheels in, did ya laddy!$3#$b#Aye I do believe this'll be the talk of the Stardrop for many Fridays to come!$1");
                Game1.player.changeFriendship(240, n);
                n.doEmote(20); // hearts
                this.questComplete();
            }
            else if (n?.Name == "Willy" && this.State == HarpoonQuestState.GetThePole && Game1.player.currentLocation.Name == "FishShop")
            {
                this.Spout(n, "Ah laddy...  I do think I know what you mighta hooked into and yer right that ya need a lot more pole than what you got.#$b#Here's a wee bit o' fishin' kit that my great great grandpappy used to land whales back before we knew better.#$b#I think ya will find it fit for tha purpose.");

                Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create(WatererQuestController.HarpoonToolId));
                this.State = HarpoonQuestState.CatchTheBigOne;
            }
            else if (n?.Name == "Willy" && this.State == HarpoonQuestState.GetThePole && Game1.player.currentLocation.Name != "FishShop")
            {
                this.Spout(n, "Ah laddy...  I do think I know what you mighta hooked into and yer right that ya need a lot more pole than what you got.#$b#Come visit me in my shop and I'll show you something that might work");
            }
        }

        internal static void OnDayStarted(ModEntry questSetup)
        {
            Game1.player.modData.TryGetValue(ModDataKeys.BorrowHarpoonQuestStatus, out string statusAsString);
            if (statusAsString is null || !Enum.TryParse(statusAsString, out HarpoonQuestState state))
            {
                return;
            }

            var quest = new BorrowHarpoonQuest(state);
            quest.MarkAsViewed();
            Game1.player.questLog.Add(quest);
            quest.MakeSoundOnAdvancement = true;
        }

        internal static void OnDayEnding()
        {
            var quest = Game1.player.questLog.OfType<BorrowHarpoonQuest>().FirstOrDefault();
            if (quest is not null)
            {
                Game1.player.modData[ModDataKeys.BorrowHarpoonQuestStatus] = quest.State.ToString();
            }
            else
            {
                Game1.player.modData.Remove(ModDataKeys.BorrowHarpoonQuestStatus);
            }
            Game1.player.questLog.RemoveWhere(q => q is BorrowHarpoonQuest);
        }

        internal static void StartQuest()
        {
            if (!Game1.player.questLog.OfType<BorrowHarpoonQuest>().Any())
            {
                Game1.addHUDMessage(new HUDMessage("Whoah, I snagged onto something big down there, but this line's nowhere near strong enough to yank it up!", HUDMessage.newQuest_type));
                var quest = new BorrowHarpoonQuest() { MakeSoundOnAdvancement = true };
                quest.ShouldDisplayAsNew();
                Game1.player.questLog.Add(new BorrowHarpoonQuest() { MakeSoundOnAdvancement = true });
                Game1.playSound("questcomplete"); // Note documentation suggests its for quest complete and "journal update".  That's what we are using it for.
            }
        }
    }
}
