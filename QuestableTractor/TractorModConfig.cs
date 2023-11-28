using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.Buildings;

namespace NermNermNerm.Stardew.QuestableTractor
{
    internal class TractorModConfig
    {
        private readonly ModEntry mod;
        private bool isTractorEnabled = true; // <- tractormod creates the tractor in OnDayStart
        private bool isBuildingAvailable = true; // <-- again, this is what tractor mod enables from day 1

        public const string GarageBuildingId = "Pathoschild.TractorMod_Stable";

        public TractorModConfig(ModEntry mod)
        {
            this.mod = mod;
        }

        public bool IsGarageBuildingAvailable
        {
            get => this.isBuildingAvailable;
            set
            {
                if (value != this.isBuildingAvailable)
                {
                    this.isBuildingAvailable = value;
                    this.mod.Helper.GameContent.InvalidateCache("Data/Buildings");
                }
            }
        }

        public void SetConfig(bool isBuildingAvailable, bool isTractorEnabled, bool isHoeUnlocked, bool isLoaderUnlocked, bool isHarvesterUnlocked, bool isWatererUnlocked, bool isSpreaderUnlocked)
        {
            if (isBuildingAvailable != this.isBuildingAvailable)
            {
                this.isBuildingAvailable = isBuildingAvailable;
                this.mod.Helper.GameContent.InvalidateCache("Data/Buildings");
            }

            this.isTractorEnabled = isTractorEnabled; // Enforced in OnDayStart
        }

        internal void OnDayStarted()
        {
            // TractorMod creates a tractor on day start.  We remove it if it's not configured.  Otherwise, doing nothing is the right thing.
            if (!this.isTractorEnabled)
            {
                Farm farm = Game1.getFarm();
                var tractorIds = farm.buildings.OfType<Stable>().Where(s => s.buildingType.Value == GarageBuildingId).Select(s => s.HorseId).ToHashSet();
                var horses = farm.characters.OfType<Horse>().Where(h => tractorIds.Contains(h.HorseId)).ToList();
                foreach (var tractor in horses)
                {
                    farm.characters.Remove(tractor);
                }
            }
        }

        internal void EditBuildings(IAssetData editor)
        {
            // TODO: It'd be nice if we could do  ' if (this.IsQuestReadyForTractorBuilding())'
            //   but it looks like the game builds its list of buildings before a save is even
            //   loaded, so we can't use any sort of context here.

            // Note that the cost isn't configurable here because:
            //  1. The whole idea of the quest is to tune it to other events in the game.
            //  2. There are several other quest objectives that have requirements besides
            //     the garage and doing them all would be kinda out of hand.
            //  3. The requirements are designed to be very manageable.  People who just
            //     want an easy button tractor should just nerf the requirements in non-quest
            //     mode.
            //
            // Note that the practical length limit of the mats list is 3 - because of the size of
            //   the shop-for-buildings dialog at Robin's shop.  It'd be nice if we could make
            //   a bit of a story out of the cup of coffee.

            if (editor.AsDictionary<string, BuildingData>().Data.TryGetValue(GarageBuildingId, out BuildingData? value))
            {
                value.BuildCost = 350;
                value.BuildMaterials = new List<BuildingMaterial>
                {
                    new BuildingMaterial() { ItemId = "(O)388", Amount = 3 }, // 3 Wood
                    new BuildingMaterial() { ItemId = "(O)390", Amount = 5 }, // 5 Stone
                    new BuildingMaterial() { ItemId = "(O)395", Amount = 1 }, // 1 cup of coffee
                };
                value.Builder = (this.isBuildingAvailable ? "Robin" : null);
            }
            else
            {
                this.mod.Monitor.Log($"It looks like TractorMod is not loaded - {GarageBuildingId} does not exist", LogLevel.Error);
            }
        }
    }
}
