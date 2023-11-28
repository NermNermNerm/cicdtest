using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Tools;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public class WatererQuestController
        : BaseQuestController<WatererQuestState, WatererQuest>
    {
        public static bool hasPatchBeenInstalled = false;

        public const string HarpoonToolId = "NermNermNerm.QuestableTractor.Harpoon";

        public WatererQuestController(QuestSetup mod)
            : base(mod)
        {
        }

        public static float chanceOfCatchingQuestItem = 0;

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

        protected override void OnQuestStarted()
        {
            chanceOfCatchingQuestItem = 0;
        }

        public override void OnDayStarted()
        {
            chanceOfCatchingQuestItem = 0;
            if (this.IsStarted)
            {
                chanceOfCatchingQuestItem = 0; // No chance - already pulled it up.
                // the docs for Harmony.Unpatch make it seem like a dangerous thing to do,
                // so we'll leave the patch on even when we know it's useless.
            }
            else
            {
                if (RestoreTractorQuest.IsTractorUnlocked)
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
                    this.mod.Harmony.Patch(getFishMethod, prefix: new HarmonyMethod(typeof(WatererQuestController), nameof(Prefix_GetFish)));
                    hasPatchBeenInstalled = true;
                }
            }
            base.OnDayStarted();
        }

        private static bool Prefix_GetFish(ref Item __result)
        {
            if (Game1.player.currentLocation is not Farm)
            {
                return false;
            }

            const string TrashItemId = "(O)168";
            // TODO: Maybe it'd be cool to remember where the thing was hooked and only boost the odds like
            // this if you're fishing in the same spot where you hooked it when the quest started.
            if (Game1.player.CurrentTool?.ItemId == HarpoonToolId && chanceOfCatchingQuestItem > 0)
            {
                if (Game1.random.NextDouble() < .3)
                {
                    Game1.playSound("submarine_landing");
                    BorrowHarpoonQuest.GotTheBigOne();
                    __result = ItemRegistry.Create(ObjectIds.BustedWaterer);
                }
                else
                {
                    Game1.playSound("clank");
                    string message = new string[]
                    {
                        "Aaahhh! ! I had it!",
                        "Nope...  nothing",
                        "OOoh so close."
                    }[Game1.random.Next(3)];
                    Game1.addHUDMessage(new HUDMessage(message) { noIcon = true });

                    __result = ItemRegistry.Create(TrashItemId);
                }
                return false;
            }
            else if (Game1.random.NextDouble() < chanceOfCatchingQuestItem)
            {
                __result = ItemRegistry.Create(TrashItemId);
                BorrowHarpoonQuest.StartQuest();
                return false;
            }
            else
            {
                return true;
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
                Texture = "Mods/QuestableTractorMod/assets/QuestSprites",
                SpriteIndex = 14,
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
