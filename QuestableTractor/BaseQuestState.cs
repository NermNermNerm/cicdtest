using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;

// Delete this if you see it around and unused later...  It's probably not useful.

namespace NermNermNerm.Stardew.QuestableTractor
{
    /// <summary>
    ///   Maintains the state for a quest.  It assumes that all the data for the quest
    ///   can be stored in a single ModData string on the main player.
    /// </summary>
    public abstract class BaseQuestState
    {
        protected BaseQuestState(string modKey)
        {
            this.ModDataKey = modKey;
        }

        protected const string QuestCompleteStateMagicWord = "Complete";

        protected string ModDataKey { get; }

        protected string? Value
        {
            get
            {
                Game1.player.modData.TryGetValue(this.ModDataKey, out string? value);
                return value;
            }
            set
            {
                if (value is null)
                {
                    Game1.player.modData.Remove(this.ModDataKey);
                }
                else
                {
                    Game1.player.modData[this.ModDataKey] = value;
                }
            }
        }

        public abstract bool CheckValidity(out string? errorMessage);

        public virtual bool IsStarted
        {
            get
            {
                return this.Value is null;
            }
        }

        public virtual bool IsCompleted
        {
            get
            {
                return this.Value == QuestCompleteStateMagicWord;
            }
        }

        public virtual void MarkComplete()
        {
            this.Value = QuestCompleteStateMagicWord;
        }
    }

    /// <summary>
    ///   A <see cref="BaseQuestState"/> implementation where the state can be encapsulated by
    ///   a single enum.
    /// </summary>
    /// <typeparam name="TEnum">
    ///   The enumeration - the default must reflect a "NotStarted" state.  If there is a "Completed"
    ///   value, then that will be the value if <see cref="MarkComplete"/> has been called.
    /// </typeparam>
    public class EnumBasedQuestState<TEnum> : BaseQuestState
         where TEnum : struct
    {
        public EnumBasedQuestState(string modKey) : base(modKey) { }

        public new TEnum Value
        {
            get => Enum.TryParse<TEnum>(base.Value, out var enumValue) ? enumValue : default;
            set => base.Value = value.ToString();
        }

        public override bool CheckValidity(out string? errorMessage)
        {
            string? v = base.Value;
            if (v is not null && !Enum.TryParse<TEnum>(v, out _))
            {
                errorMessage = "Invalid value: {v}";
                return false;
            }
            else
            {
                errorMessage = null;
                return true;
            }
        }
    }
}
