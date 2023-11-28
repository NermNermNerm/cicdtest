using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.Quests;

namespace NermNermNerm.Stardew.QuestableTractor
{
    public class RestoreTractorQuest
        : Quest
    {
        private RestorationState state = RestorationState.NotStarted;

        private bool hasDoneStatusCheckToday = false;

        public RestoreTractorQuest()
            : this(RestorationState.TalkToLewis)
        {
            this.showNew.Value = true;
        }

        public RestoreTractorQuest(RestorationState state)
        {
            this.questTitle = "Investigate the tractor";
            this.questDescription = "There's a rusty old tractor in the fields; it sure would be nice if it could be restored.  Perhaps the townspeople can help.";
            this.SetState(state);
        }

        public static bool IsBuildingUnlocked
        {
            get => QuestSetup.GetModConfig<RestorationState>(ModDataKeys.MainQuestStatus) >= RestorationState.BuildTractorGarage;
        }

        public static bool IsTractorUnlocked
        {
            get => QuestSetup.GetModConfig<RestorationState>(ModDataKeys.MainQuestStatus) == RestorationState.Complete;
        }

        public static bool IsStarted
        {
            get => QuestSetup.GetModConfig<RestorationState>(ModDataKeys.MainQuestStatus) != RestorationState.NotStarted;
        }

        private void SetState(RestorationState state)
        {
            this.state = state;

            switch (state)
            {
                case RestorationState.TalkToLewis:
                    this.currentObjective = "Talk to mayor Lewis";
                    break;

                case RestorationState.TalkToSebastian:
                    this.currentObjective = "Ask Sebastian to help restore the tractor";
                    break;

                case RestorationState.TalkToLewisAgain:
                    this.currentObjective = "Welp, Sebastian was a bust.  Maybe Mayor Lewis knows somebody else who could be more helpful.";
                    break;

                case RestorationState.WaitingForMailFromRobinDay1:
                case RestorationState.WaitingForMailFromRobinDay2:
                    this.currentObjective = "Wait for Lewis to work his magic";
                    break;

                case RestorationState.BuildTractorGarage:
                    this.currentObjective = "Get Robin to build you a garage to get the tractor out of the weather.";
                    break;

                case RestorationState.WaitingForSebastianDay1:
                case RestorationState.WaitingForSebastianDay2:
                    this.currentObjective = "Sebastian promised to get on the job right after the barn got built.  Hopefully he's actually on the case.";
                    break;

                case RestorationState.TalkToWizard:
                    this.currentObjective = "Ask for help with the strange tractor motor.";
                    break;

                case RestorationState.BringStuffToForest:
                    this.currentObjective = "Put the motor, the bat wing, 20 sap, 20 mixed seeds and an Aquamarine in a chest in the secret woods.";
                    break;

                case RestorationState.BringEngineToSebastian:
                    this.currentObjective = "Hopefully the Junimo magic worked!  Get the engine out of the secret woods and bring it to Sebastian.";
                    break;

                case RestorationState.BringEngineToMaru:
                    this.currentObjective = "Bring the engine to Maru to install.";
                    break;

                case RestorationState.WaitForEngineInstall:
                    this.currentObjective = "Maru says that after the engine is installed, it should actually run!  Just have to wait a little bit longer...";
                    break;
            }
        }

        public static RestorationState AdvanceProgress(Stable? garage, RestorationState restorationStatus)
        {

            // Advance progress
            switch (restorationStatus)
            {
                case RestorationState.WaitingForMailFromRobinDay1:
                    restorationStatus = RestorationState.WaitingForMailFromRobinDay2;
                    break;
                case RestorationState.WaitingForMailFromRobinDay2:
                    restorationStatus = RestorationState.BuildTractorGarage;
                    Game1.addMail(MailKeys.BuildTheGarage);
                    break;
                case RestorationState.BuildTractorGarage:
                    if (garage is not null && !garage.isUnderConstruction())
                    {
                        restorationStatus = RestorationState.WaitingForSebastianDay1;
                    }
                    break;
                case RestorationState.WaitingForSebastianDay1:
                    restorationStatus = RestorationState.WaitingForSebastianDay2;
                    break;
                case RestorationState.WaitingForSebastianDay2:
                    Game1.addMail(MailKeys.FixTheEngine);
                    restorationStatus = RestorationState.TalkToWizard;
                    break;
                case RestorationState.BringStuffToForest:
                    if (CheckForest())
                    {
                        restorationStatus = RestorationState.BringEngineToSebastian;
                    }
                    break;
                case RestorationState.WaitForEngineInstall:
                    Game1.player.mailbox.Add(MailKeys.TractorDoneMail);
                    restorationStatus = RestorationState.Complete;
                    break;
            }

            return restorationStatus;
        }

        public static bool CheckForest()
        {
            var forest = Game1.getLocationFromName("Woods");
            bool hasSap = false, hasEngine = false, hasSeeds = false, hasGem = false;
            foreach (var chest in forest.objects.Values.OfType<Chest>())
            {
                foreach (var item in chest.Items)
                {
                    if (item.ItemId == "92" && item.Stack >= 20)
                    {
                        hasSap = true;
                    }
                    if (item.ItemId == ObjectIds.BustedEngine)
                    {
                        hasEngine = true;
                    }
                    if (item.ItemId == "770" && item.Stack >= 20)
                    {
                        hasSeeds = true;
                    }
                    if (item.ItemId == "62")
                    {
                        hasGem = true;
                    }
                }
            }

            if (!hasSap || !hasEngine || !hasSeeds || !hasGem)
            {
                return false;
            }

            foreach (var chest in forest.objects.Values.OfType<Chest>())
            {
                List<Item> toRemove = new List<Item>();
                List<Item> toAdd = new List<Item>();
                foreach (var item in chest.Items)
                {
                    if (item.ItemId == "92" && item.Stack >= 20)
                    {
                        toRemove.Add(item); // Keeping it simple: If you give the Junimo's more than 20, that's like tipping them.
                    }
                    if (item.ItemId == ObjectIds.BustedEngine)
                    {
                        toRemove.Add(item);
                        toAdd.Add(new StardewValley.Object(ObjectIds.WorkingEngine, 1));
                    }
                    if (item.ItemId == "770" && item.Stack >= 20)
                    {
                        toRemove.Add(item);
                    }
                    if (item.ItemId == "62")
                    {
                        toRemove.Add(item);
                    }
                }

                foreach (var deadItem in toRemove)
                {
                    chest.Items.Remove(deadItem);
                }
                chest.Items.AddRange(toAdd);
            }

            return true;
        }

        public static void OnDayStarted(QuestSetup mod)
        {
            if (!Game1.player.modData.TryGetValue(ModDataKeys.MainQuestStatus, out string? statusAsString)
                || !Enum.TryParse(statusAsString, true, out RestorationState mainQuestStatusAtDayStart))
            {
                if (statusAsString is not null)
                {
                    mod.Monitor.Log($"Invalid value for {ModDataKeys.MainQuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                }
                mainQuestStatusAtDayStart = RestorationState.NotStarted;
            }

            var garage = Game1.getFarm().buildings.OfType<Stable>().FirstOrDefault(s => s.buildingType.Value == QuestSetup.GarageBuildingId);

            var mainQuestStatus = RestoreTractorQuest.AdvanceProgress(garage, mainQuestStatusAtDayStart);

            if (mainQuestStatus.IsDerelictInTheFields())
            {
                DerelictTractorTerrainFeature.PlaceInField(mod);
            }
            else if (mainQuestStatus.IsDerelictInTheGarage())
            {
                if (garage is null || garage.isUnderConstruction())
                {
                    // Could happen I suppose if the user deleted the garage while on the quest.  They can fix it themselves by rebuilding the garage...
                    mod.Monitor.Log($"Tractor main quest state is {mainQuestStatus} but there's no garage??", LogLevel.Error);
                }
                else
                {
                    DerelictTractorTerrainFeature.PlaceInGarage(mod, garage);
                }
            }

            if (mainQuestStatus != RestorationState.Complete && mainQuestStatus != RestorationState.NotStarted)
            {
                var q = new RestoreTractorQuest(mainQuestStatus);
                q.MarkAsViewed();
                Game1.player.questLog.Add(q);
            }
            else if (mainQuestStatus == RestorationState.Complete && mainQuestStatusAtDayStart != RestorationState.Complete)
            {
                var q = new RestoreTractorQuest(mainQuestStatus);
                Game1.player.questLog.Add(q);
                q.questComplete();
                Game1.player.modData[ModDataKeys.MainQuestStatus] = RestorationState.Complete.ToString();
            }
        }

        public string Serialize() => this.state.ToString();

        public static void Spout(NPC n, string message)
        {
            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n);
        }

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if (n?.Name == "Lewis" && this.state == RestorationState.TalkToLewis)
            {
                Spout(n, "An old tractor you say?#$b#I know your Grandfather had one - I thought he had sold it off before he died.  He never could keep it on the furrows.$h#$b#If you want to get it fixed, I suggest you talk to Robin's son, Sebastian; he's actually quite the gearhead.  Maybe he can help.");
                this.SetState(RestorationState.TalkToSebastian);
            }
            else if (n?.Name == "Sebastian" && this.state == RestorationState.TalkToSebastian)
            {
                Spout(n, "Let me get this straight - I barely know who you are and I'm supposed to fix your rusty old tractor?$a#$b#Sorry, but I've got a lot of stuff going on and can't really spare the time.");
                Game1.drawDialogue(n);
                this.SetState(RestorationState.TalkToLewisAgain);
            }
            else if (n?.Name == "Lewis" && this.state == RestorationState.TalkToLewisAgain)
            {
                Spout(n, "He said that?$a#$b#Well, I can't say I'm really surprised...  A bit disappointed, tho.$s#$b#Hm. . .$u#$b#Welp, I guess this is why they pay me the big money, eh?  I'll see if I can make this happen for you, but it might take a couple days.");
                Game1.drawDialogue(n);
                this.SetState(RestorationState.WaitingForMailFromRobinDay1);
            }
            // Maybe make an "if there's coffee involved it goes faster?" option?
            else if (n?.Name == "Sebastian" && this.state == RestorationState.WaitingForSebastianDay1 && !this.hasDoneStatusCheckToday)
            {
                Spout(n, "Trust me, I'm working on it, but I also have my day-gig to worry about.  I work odd hours, so you might not be around when I'm working on it.  Oh and thanks for the coffee.");
                Game1.drawDialogue(n);
                this.hasDoneStatusCheckToday = true;
            }
            else if (n?.Name == "Sebastian" && this.state == RestorationState.WaitingForSebastianDay2 && !this.hasDoneStatusCheckToday)
            {
                Spout(n, "I made a lot of progress last night.  Most of it is cleaning up okay, but the engine itself is, well, it seems a little out of the ordinary. . .");
                Game1.drawDialogue(n);
                this.hasDoneStatusCheckToday = true;
            }
            else if (n?.Name == "Wizard" && this.state == RestorationState.TalkToWizard && item?.ItemId == ObjectIds.BustedEngine)
            {
                Spout(n, "Oh...  Now where did you get that??!$l#$b#Ooooh... Ah.  Yes.  I see...  Mmm...$s#$b#Yes.  Your grandfather dabbled a bit in Forest Magic.  He was nowhere near as adept as myself, of course...#$b#He lacked the mechanical ability to restore the mundane engine, so he enlisted some forest magic to make one.#$b#As you can see, the Junimos that he recruited to keep the motor running have gotten bored and wandered away.  You'll need to coax them back.$s#$b#Now, pay attention!  This will require your utmost concentration!$a#$b#You must place the engine, 20 sap, 20 mixed seeds, and an aquamarine in a chest in the secret woods in front of the statue...#$b#Then, you must run around the chest, six times, clockwise very, very quickly.  Overnight, your engine will be restored.#$b#Now GO!  I have concerns much greater than yours right now.$a");
                this.SetState(RestorationState.BringStuffToForest);
            }
            else if (n?.Name == "Sebastian" && item?.ItemId == ObjectIds.BustedEngine)
            {
                Spout(n, "That is the craziest engine I've ever seen.  Have  you shown it to Clint?  I mean, he knows something about metalworking.  Maybe it's some kinda wierd alloy?^ ^Or...^Maybe aliens.");
            }
            else if (n?.Name == "Clint" && item?.ItemId == ObjectIds.BustedEngine)
            {
                Spout(n, "Uh...#$b#What is it?  you say it's an Engine?$s#$b#I say it's wierd. . .  Hey, is that thing moving?$a#$b#I don't know.  Maybe the Wizard would know what it is, and even if he doesn't, he'll sure pretend like he does if you show it to him.$");
            }
            else if ((n?.Name == "Abigail" || n?.Name == "Vincent") && item?.ItemId == ObjectIds.BustedEngine)
            {
                Spout(n, "Oh wow...#$b#Can I have it?");
            }
            else if (n?.Name == "Marnie" && item?.ItemId == ObjectIds.BustedEngine)
            {
                Spout(n, "AAAAHHH!!!  IT'S MOVING!  TAKE IT AWAY!$4");
                // TODO: Remember that Marnie saw it and have gossip later about it.
            }
            else if (n is not null && item?.ItemId == ObjectIds.BustedEngine)
            {
                Spout(n, "I've never seen anything like that before...#$b#It gives me this uncanny feeling like...  it's missing something.#$b#Wierd.");
            }
            else if (n?.Name == "Sebastian" && item?.ItemId == ObjectIds.WorkingEngine)
            {
                Spout(n, "Whoah....$s#$b#I mean, if you say it's fixed, I can believe it.  Definitely has a look of workiness about it!$l#$b#But seriously...  I shouldn't be installing this thing.  It's, yaknow, out of my area but...$s#$b#I hate to say it, my Sister would be able to figure it out, no matter how wierd it is.");
                this.SetState(RestorationState.BringEngineToMaru);
            }
            else if (n?.Name == "Maru" && item?.ItemId == ObjectIds.WorkingEngine)
            {
                Spout(n, "Wow!  I mean I have no idea what it does, but I'm sure it'll look cool doing it!$h#$b#You want me to install it in the tractor?  Sure, I'll do it.  I helped Seb haul it out of the mud.  He really did a great job polishing it up.#$b#Just give me a day or so, k?  And be sure to drive it up here sometime, I want to ride it around!$l");
                this.SetState(RestorationState.WaitForEngineInstall);
                Game1.player.removeItemFromInventory(item);
            }

            return base.checkIfComplete(n, number1, number2, item, str, probe);
        }
    }
}
