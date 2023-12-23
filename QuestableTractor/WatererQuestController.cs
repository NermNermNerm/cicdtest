using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Tools;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public class WatererQuestController
        : TractorPartQuestController<WatererQuestState>
    {
        public static bool hasPatchBeenInstalled = false;

        public const string HarpoonToolId = "NermNermNerm.QuestableTractor.Harpoon";

        public WatererQuestController(ModEntry mod)
            : base(mod)
        {
        }

        public static float chanceOfCatchingQuestItem = 0;

        protected override WatererQuest CreateQuest() => new WatererQuest(this);

        protected override string QuestCompleteMessage => "Awesome!  You've now got a way to water your crops with your tractor!#$b#HINT: To use it, equip the watering can while on the tractor.";

        protected override string ModDataKey => ModDataKeys.WateringQuestStatus;

        public override string WorkingAttachmentPartId => ObjectIds.WorkingWaterer;

        public override string BrokenAttachmentPartId => ObjectIds.BustedWaterer;

        public override string HintTopicConversationKey => ConversationKeys.WatererNotFound;

        public override void AnnounceGotBrokenPart(Item brokenPart)
        {
            // We want to act a lot differently than we do in the base class, as we got the item through fishing, holding it up would look dumb
            Spout("Whoah that was heavy!  Looks like an irrigator attachment for a tractor under all that mud!");
        }

        protected override void MonitorQuestItems()
        {
            chanceOfCatchingQuestItem = 0;
            base.MonitorQuestItems();
        }

        protected override void HideStarterItemIfNeeded()
        {
            if (this.Mod.RestoreTractorQuestController.IsComplete)
            {
                chanceOfCatchingQuestItem = 0.01f + Game1.Date.TotalDays / 200f;
            }
            else
            {
                chanceOfCatchingQuestItem = .01f;
            }

            if (!hasPatchBeenInstalled)
            {
                // Undoing a Harmony patch is sketchy, so we're going to go ahead and install our patch even if the quest is irrelevant.
                // It might be wiser to not do it until we know the quest hasn't been started
                var farmType = typeof(Farm);
                var getFishMethod = farmType.GetMethod("getFish");
                WatererQuestController.instance = this; // Harmony doesn't support creating prefixes with instance methods...  Faking it.
                this.Mod.Harmony.Patch(getFishMethod, prefix: new HarmonyMethod(typeof(WatererQuestController), nameof(Prefix_GetFish)));
                hasPatchBeenInstalled = true;
            }
        }

        private static WatererQuestController instance = null!;

        private static bool Prefix_GetFish(ref Item __result)
        {
            var newFish = instance.ReplaceFish();
            if (newFish is null)
            {
                return true; // Go ahead and call the normal function.
            }
            else
            {
                __result = newFish;
                return false; // Skip calling the function and use this result.
            }
        }

        private Item? ReplaceFish()
        {
            if (Game1.player.currentLocation is not Farm)
            {
                return null;
            }

            const string TrashItemId = "(O)168";

            // Consider: Maybe it'd be cool to remember where the thing was hooked and only boost the odds like
            // this if you're fishing in the same spot where you hooked it when the quest started.
            if (Game1.player.CurrentTool?.ItemId == HarpoonToolId && chanceOfCatchingQuestItem > 0)
            {
                var borrowHarpoonQuest = Game1.player.questLog.OfType<BorrowHarpoonQuest>().FirstOrDefault();
                if (borrowHarpoonQuest is null)
                {
                    this.LogError("BorrowHarpoon quest was not open when player caught waterer");
                    return null;
                }

                if (Game1.random.NextDouble() < .3)
                {
                    Game1.playSound("submarine_landing");
                    borrowHarpoonQuest.LandedTheWaterer();
                    return ItemRegistry.Create(ObjectIds.BustedWaterer);
                }
                else
                {
                    Game1.playSound("clank");
                    string message = new string[]
                    {
                        "Aaahhh! ! I had it!",
                        "Nope...  nothing",
                        "Ooohhh!  So close!"
                    }[Game1.random.Next(3)];
                    Game1.addHUDMessage(new HUDMessage(message) { noIcon = true });

                    return ItemRegistry.Create(TrashItemId);
                }
            }
            else if (Game1.random.NextDouble() < chanceOfCatchingQuestItem)
            {
                this.Mod.BorrowHarpoonQuestController.StartQuest();
                return ItemRegistry.Create(TrashItemId);
            }
            else
            {
                return null;
            }
        }

        protected override WatererQuestState AdvanceStateForDayPassing(WatererQuestState oldState)
        {
            if (oldState == WatererQuestState.WaitForMaruDay1)
            {
                Game1.player.mailForTomorrow.Add(MailKeys.WatererRepaired);
                return WatererQuestState.WaitForMaruDay2;
            }
            else
            {
                return oldState;
            }
        }

        internal static void EditToolAssets(IDictionary<string, ToolData> data)
        {
            data[HarpoonToolId] = new ToolData
            {
                ClassName = "FishingRod",
                Name = "Harpoon",
                AttachmentSlots = 0,
                SalePrice = 0,
                DisplayName = "Great Grandpappy's Harpoon",
                Description = "Willy's Great Grandpappy used this to hunt whales back in the day.",
                Texture = ModEntry.SpritesPseudoPath,
                SpriteIndex = 19,
                MenuSpriteIndex = -1,
                UpgradeLevel = 0,
                ApplyUpgradeLevelToDisplayName = false,
                ConventionalUpgradeFrom = null,
                UpgradeFrom = null,
                CanBeLostOnDeath = false,
                SetProperties = null,
            };
        }
    }
}
