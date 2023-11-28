using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace NermNermNerm.Stardew.QuestableTractor
{
    internal class TractorModConfig
    {
        private readonly QuestSetup mod;
        private bool isTractorEnabled = true; // <- tractormod creates the tractor in OnDayStart

        public TractorModConfig(QuestSetup mod)
        {
            this.mod = mod;
        }

        public void SetConfig(bool isBuildingAvailable, bool isTractorEnabled, bool isHoeUnlocked, bool isLoaderUnlocked, bool isHarvesterUnlocked, bool isWatererUnlocked, bool isSpreaderUnlocked)
        {
            this.isTractorEnabled = isTractorEnabled; // Enforced in OnDayStart
        }

        internal void OnDayStarted()
        {
            // TractorMod creates a tractor on day start.  We remove it if it's not configured.  Otherwise, doing nothing is the right thing.
            if (!this.isTractorEnabled)
            {
                Farm farm = Game1.getFarm();
                var tractorIds = farm.buildings.OfType<Stable>().Where(s => s.buildingType.Value == QuestSetup.GarageBuildingId).Select(s => s.HorseId).ToHashSet();
                var horses = farm.characters.OfType<Horse>().Where(h => tractorIds.Contains(h.HorseId)).ToList();
                foreach (var tractor in horses)
                {
                    farm.characters.Remove(tractor);
                }
            }
        }
    }
}
