using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StardewValley;
using StardewValley.Quests;

namespace NermNermNerm.Stardew.QuestableTractor
{

    public abstract class BaseQuest<TStateEnum> : Quest
        where TStateEnum : struct, Enum
    {
        private TStateEnum state;

        protected BaseQuest(TStateEnum state)
        {
            this.state = state;
            this.SetObjective();
        }

        /// <summary>
        ///   Used to disable the sound made by <see cref="IndicateQuestHasMadeProgress"/> during automated unpacking
        ///   and start-of-day changes.  It starts out false and then gets made true after the quest is deserialized
        /// </summary>
        public bool MakeSoundOnAdvancement { get; internal set; } = false;

        public TStateEnum State
        {
            get => this.state;
            set
            {
                if (!value.Equals(this.state))
                {
                    this.IndicateQuestHasMadeProgress();
                }
                this.state = value;
                this.SetObjective();
            }
        }

        public BaseQuestController Controller { get; internal set; } = null!;

        /// <summary>
        ///  Called on either actual or possible interaction with an NPC that could have bearing on a quest.
        /// </summary>
        /// <param name="n">The NPC the player is interacting with - can be null.</param>
        /// <param name="number1"></param>
        /// <param name="number2"></param>
        /// <param name="item"></param>
        /// <param name="str">Seems the same as <paramref name="item"/>.Name in all cases.  Perhaps a holdover from an earlier iteration.</param>
        /// <param name="probe">
        ///   True if the player is hovering over the NPC and (Utility.checkForCharacterInteractionAtTile) and the
        ///   object the player is holding can be given as a gift to that NPC.
        /// </param>
        /// <returns>
        ///  <para>
        ///   Documentation exists for NPC.tryToReceiveActiveObject, which says: Whether to return what the method would
        ///   return if called normally, but without actually accepting the item or making any changes to the NPC. This
        ///   is used to accurately predict whether the NPC would accept or react to the offer.
        ///  </para>
        ///  <para>
        ///   In Utility.checkForCharacterInteractionAtTile (which will call with probe=true), true seems to mean
        ///   that the 'gift' cursor will be used if the player is holding something related to the quest.  But that
        ///   seems odd because the object has to be giftable to even get to the point where it probes.
        ///  </para>
        ///  <para>
        ///   The return value is ignored in several places.  All-up, I think it is meant to convey a "doneness",
        ///   or with <paramref name="probe"/>, whether it's interesting.  For multi-stage quests like what's here,
        ///   the return value could maybe be used to indicate stage-completion, but I don't believe there's ever
        ///   a case in the code where the return value matters.
        ///  </para>
        /// </returns>
        public override sealed bool checkIfComplete(NPC n, int number1, int number2, Item item, string str, bool probe)
        {
            if (probe || n is null)
            {
                return false;
            }

            if (item is not null && !this.IsItemForThisQuest(item))
            {
                return false;
            }

            this.CheckIfComplete(n, item);
            return false;
        }

        public abstract void CheckIfComplete(NPC n, Item? item);

        public void IndicateQuestHasMadeProgress()
        {
            if (this.MakeSoundOnAdvancement)
            {
                Game1.playSound("questcomplete"); // Note documentation suggests its for quest complete and "journal update".  That's what we are using it for.
            }
        }

        protected virtual bool IsItemForThisQuest(Item item) => item?.ItemId == this.Controller.BrokenAttachmentPartId;

        protected abstract void SetObjective();

        public abstract void GotWorkingPart(Item workingPart);

        // Putting this implementation here denies a few other usages, and it also means that our suppressions are
        //  tied to the quest, and thus get tossed out every day.  I can't say if that's a bug or a feature right now.
        private HashSet<string> oldNews = new HashSet<string>();

        public void Spout(NPC n, string message)
        {
            // This only impacts quest-based messages, and the 'oldNews' thing gets reset once per day.  Not sure if
            // the once-per-day thing is a bug or a feature.
            if (!this.oldNews.Add(n.Name + message))
            {
                return;
            }

            // Conversation keys and location specific dialogs take priority.  We can't fix the location-specific
            // stuff, but we can nix conversation topics.

            // Forces it to see if there are Conversation Topics that can be pulled down.
            // Pulling them down toggles their "only show this once" behavior.
            n.checkForNewCurrentDialogue(Game1.player.getFriendshipHeartLevelForNPC(n.Name));

            // TODO: Only nix topics that are for this mod.
            // Can (maybe) be culled off of the tail end of 'n.CurrentDialogue.First().TranslationKey'
            n.CurrentDialogue.Clear();

            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n); // <- Push'ing or perhaps the clicking on the NPC causes this to happen anyway. so not sure if it actually helps.
        }

        public void Spout(string message) => BaseQuestController.Spout(message);

        public virtual string Serialize() => this.state.ToString();

        protected void AddItemToInventory(string itemId)
        {
            // TODO: Make it scatter the item to litter if no room in inventory
            _ = Game1.player.addItemToInventory(new StardewValley.Object(itemId, 1));
        }

        protected bool TryTakeItemsFromPlayer(string itemId, int count = 1)
        {
            var stack = Game1.player.Items.FirstOrDefault(i => i?.ItemId == itemId && i.stack.Value >= count);
            if (stack == null)
            {
                return false;
            }
            else if (stack.Stack == count)
            {
                Game1.player.removeItemFromInventory(stack);
                return true;
            }
            else
            {
                stack.Stack -= 3;
                return true;
            }
        }

        protected bool TryTakeItemsFromPlayer(string item1Id, int count1, string item2Id, int count2)
        {
            var stack1 = Game1.player.Items.FirstOrDefault(i => i?.ItemId == item1Id && i.stack.Value >= count1);
            var stack2 = Game1.player.Items.FirstOrDefault(i => i?.ItemId == item2Id && i.stack.Value >= count2);
            if (stack1 is null || stack2 is null)
            {
                return false;
            }

            if (stack1.Stack == count1)
            {
                Game1.player.removeItemFromInventory(stack1);
            }
            else
            {
                stack1.Stack -= count1;
            }

            if (stack2.Stack == count2)
            {
                Game1.player.removeItemFromInventory(stack2);
            }
            else
            {
                stack2.Stack -= count2;
            }

            return true;
        }

        public virtual void AdvanceStateForDayPassing() {}
    }
}