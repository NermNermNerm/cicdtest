using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private bool isBuildingAvailable = true; // <-- again, this is what tractor mod enables from day 1

        public const string GarageBuildingId = "Pathoschild.TractorMod_Stable";
        public const string TractorModId = "Pathoschild.TractorMod";

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

        public void SetConfig(bool isHoeUnlocked, bool isLoaderUnlocked, bool isHarvesterUnlocked, bool isWatererUnlocked, bool isSpreaderUnlocked)
        {
            this.SetupTool("Axe", isLoaderUnlocked, "CutTreeStumps,ClearTreeSeeds,ClearTreeSaplings,CutBushes,ClearDebris");
            this.SetupTool("Fertilizer", isSpreaderUnlocked, "Enable");
            this.SetupTool("GrassStarter", isSpreaderUnlocked, "Enable");
            this.SetupTool("Hoe", isHoeUnlocked, "TillDirt,ClearWeeds,HarvestGinger");
            this.SetupTool("MilkPail", false, "");
            this.SetupTool("MeleeBlunt", false, "");
            this.SetupTool("MeleeDagger", false, "");
            this.SetupTool("MeleeSword", false, "");
            this.SetupTool("PickAxe", isLoaderUnlocked, "ClearDebris,ClearDirt,ClearWeeds");
            this.SetupTool("Scythe", isHarvesterUnlocked, "HarvestCrops,HarvestFlowers,HarvestGrass,HarvestForage,ClearDeadCrops,ClearWeeds");
            this.SetupTool("Seeds", isSpreaderUnlocked, "Enable");
            this.SetupTool("Shears", false, "");
            this.SetupTool("Slingshot", false, "");
            this.SetupTool("WateringCan", isWatererUnlocked, "Enable");
            this.SetupTool("SeedBagMod", isSpreaderUnlocked, "Enable");
        }

        private void SetupTool(string toolName, bool isEnabled, string enabledModes)
        {
            // This is sorta what the golden path would look like:
            // object? tractorModApi = this.mod.Helper.ModRegistry.GetApi(TractorModId)!;

            // But today...
            var modInfo = this.mod.Helper.ModRegistry.Get(TractorModId)!;
            object tractorModApi = modInfo.GetType().GetProperty("Mod")!.GetValue(modInfo)!;
            var configProp = tractorModApi.GetType().GetField("Config", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField)!;
            object? tractorModConfig = configProp.GetValue(tractorModApi)!;
            var stdAttachProp = tractorModConfig.GetType().GetProperty("StandardAttachments")!;
            object? stdAttach = stdAttachProp.GetValue(tractorModConfig)!;
            var toolProp = stdAttach.GetType().GetProperty(toolName)!;
            object? tool = toolProp.GetValue(stdAttach)!;

            var enabledModesHash = (isEnabled ? enabledModes.Split(",") : new string[0]).ToHashSet();
            foreach (var prop in tool.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty).Where(p => p.PropertyType == typeof(bool)))
            {
                prop.SetValue(tool, enabledModesHash.Contains(prop.Name));
            }
        }

        internal void OnDayStarted()
        {
            // TractorMod creates a tractor on day start.  We remove it if it's not configured.  Otherwise, doing nothing is the right thing.
            if (!RestoreTractorQuest.IsTractorUnlocked)
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
            if (editor.AsDictionary<string, BuildingData>().Data.TryGetValue(GarageBuildingId, out BuildingData? value))
            {
                // TODO: change these costs after the first build.  Not sure why anybody would want to build a second garage tho.  Multiplayer?
                value.BuildCost = 350;
                value.BuildMaterials = new List<BuildingMaterial>
                {
                    new BuildingMaterial() { ItemId = "(O)388", Amount = 300 }, // Wood
                    new BuildingMaterial() { ItemId = "(O)390", Amount = 200 }, // Stone
                    new BuildingMaterial() { ItemId = "(O)395", Amount = 1 }, // 1 cup of coffee
                };
                value.Builder = (this.isBuildingAvailable ? "Robin" : null);
            }
            else
            {
                this.mod.LogError($"It looks like TractorMod is not loaded - {GarageBuildingId} does not exist");
            }
        }
    }
}
