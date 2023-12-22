using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.GarbageCans;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public class ModEntry
        : Mod, ISimpleLog
    {
        private IReadOnlyCollection<BaseQuestController> QuestControllers = null!;
        private LoaderQuestController loaderQuestController = null!;
        private ScytheQuestController scytheQuestController = null!;
        private SeederQuestController seederQuestController = null!;
        private WatererQuestController watererQuestController = null!;

        public const string SpritesPseudoPath = "Mods/NermNermNerm/QuestableTractor/Sprites";

        public Harmony Harmony = null!;
        internal readonly TractorModConfig TractorModConfig;

        public ModEntry()
        {
            this.TractorModConfig = new TractorModConfig(this);
        }

        public override void Entry(IModHelper helper)
        {
            this.Harmony = new Harmony(this.ModManifest.UniqueID);

            this.loaderQuestController = new LoaderQuestController(this);
            this.scytheQuestController = new ScytheQuestController(this);
            this.seederQuestController = new SeederQuestController(this);
            this.watererQuestController = new WatererQuestController(this);
            this.QuestControllers = new List<BaseQuestController> { this.loaderQuestController, this.scytheQuestController, this.seederQuestController, this.watererQuestController };

            this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
            this.Helper.Events.GameLoop.SaveLoaded += (_, _) => this.UpdateTractorModConfig();
            this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            this.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        }

        void ISimpleLog.WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            if (isOnceOnly)
            {
                this.Monitor.LogOnce(message, level);
            }
            else
            {
                this.Monitor.Log(message, level);
            }
        }

        private void UpdateTractorModConfig()
        {
            this.TractorModConfig.SetConfig(
                isHoeUnlocked: true, // <- comes stock
                isLoaderUnlocked: this.loaderQuestController.IsComplete,
                isHarvesterUnlocked: this.scytheQuestController.IsComplete,
                isSpreaderUnlocked: this.seederQuestController.IsComplete,
                isWatererUnlocked: this.watererQuestController.IsComplete);
        }

        private void GameLoop_OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            bool IsPlayerInGarage(Character c, Stable b)
                => b.intersects(new Rectangle(new Point((int)c.Position.X, (int)c.Position.Y - 128), new Point(64, 128)));

            var itemInHand = Game1.player?.CurrentItem;
            if (Game1.player is not null && Game1.player.currentLocation is not null && itemInHand is not null && Game1.player.currentLocation == Game1.getFarm()
                && Game1.player.currentLocation.buildings
                    .OfType<Stable>()
                    .Where(s => s.buildingType.Value == TractorModConfig.GarageBuildingId)
                    .Any(s => IsPlayerInGarage(Game1.player, s)))
            {
                foreach (var qc in this.QuestControllers)
                {
                    if (qc.PlayerIsInGarage(itemInHand))
                    {
                        this.UpdateTractorModConfig();
                    }
                }
            }
        }

        [EventPriority(EventPriority.Low)] // Causes our OnDayStarted to come after TractorMod's, which does not set EventPriority
        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            foreach (var qc in this.QuestControllers)
            {
                qc.OnDayStarted();
            }

            RestoreTractorQuest.OnDayStarted(this);
            BorrowHarpoonQuest.OnDayStarted(this);
            this.SetupMissingPartConversations();
            this.TractorModConfig.OnDayStarted();
        }

        private void SetupMissingPartConversations()
        {
            // Our stuff recurs every week for 4 days out of the week.  Delay until after the
            // first week so that the introductions quest runs to completion.  Perhaps it
            // would be better to delay until all the villagers we care about have been greeted.
            if (Game1.Date.DayOfWeek != DayOfWeek.Sunday)
            {
                return;
            }

            // A case could be made to having code that removes these conversation keys as
            // things get found, but maybe it'd be better to figure that it takes a while for
            // word to get around...  Although there might be some awkward dialogs with
            // townspeople directly involved in the quest.

            if (!RestoreTractorQuest.IsStarted)
            {
                Game1.player.activeDialogueEvents.Add(ConversationKeys.TractorNotFound, 4);
            }
            else
            {
                string[] possibleHintTopics = this.QuestControllers
                    .Where(qc => !qc.IsStarted && qc.HintTopicConversationKey is not null)
                    .Select(qc => qc.HintTopicConversationKey!).ToArray();
                if (possibleHintTopics.Any())
                {
                    Game1.player.activeDialogueEvents.Add(possibleHintTopics[Game1.random.Next(possibleHintTopics.Length)], 4);
                }
            }
        }

        /// <summary>
        ///   Custom classes, like we're doing with the tractor and the quest, don't serialize without some help.
        ///   This method provides that help by converting the objects to player moddata and deleting the objects
        ///   prior to save.  <see cref="InitializeQuestable"/> restores them.
        /// </summary>
        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            Game1.getFarm().terrainFeatures.RemoveWhere(p => p.Value is DerelictTractorTerrainFeature);

            string? questState = Game1.player.questLog.OfType<RestoreTractorQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[ModDataKeys.MainQuestStatus] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is RestoreTractorQuest);

            foreach (var qc in this.QuestControllers)
            {
                qc.OnDayEnding();
            }

            BorrowHarpoonQuest.OnDayEnding();
        }

        public static T GetModConfig<T>(string key)
            where T: struct // <-- see how Enum.TryParse<T> is declared for evidence that's the best you can do.
        {
            return (Game1.player.modData.TryGetValue(key, out string value) && Enum.TryParse(value, out T result)) ? result : default(T);
        }

        internal void OnAssetRequested(object? _, AssetRequestedEventArgs e)
        {
            // this.Monitor.Log($"OnAssetRequested({e.NameWithoutLocale.Name})");
            if (e.NameWithoutLocale.IsEquivalentTo(SpritesPseudoPath))
            {
                e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/GarbageCans"))
            {
                e.Edit(editor => this.loaderQuestController.EditGarbageCanAsset(editor.GetData<GarbageCanData>()));
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(editor =>
                {
                    this.TractorModConfig.EditBuildings(editor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditAssets(editor.AsDictionary<string, ObjectData>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                // TODO: Can we make this recipe only available when its quest is running?
                e.Edit(editor =>
                {
                    IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;
                    recipes["TractorMod.ScytheAttachment"] = $"{ObjectIds.BustedScythe} 1 {ObjectIds.ScythePart1} 1 {ObjectIds.ScythePart2} 1/Field/{ObjectIds.WorkingScythe}/false/default/";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(editor =>
                {
                    MailKeys.EditAssets(editor.AsDictionary<string, string>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(editor =>
                {
                    WatererQuestController.EditToolAssets(editor.AsDictionary<string, ToolData>().Data);
                });
            }
            else if (e.NameWithoutLocale.StartsWith("Characters/Dialogue/"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    ConversationKeys.EditAssets(e.NameWithoutLocale, topics);
                });
            }
        }

        public static T Disabled<T>() where T : new()
        {
            var x = new T();
            foreach (var prop in typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanWrite && prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(x, false, null);
                }
            }

            return x;
        }
    }
}
