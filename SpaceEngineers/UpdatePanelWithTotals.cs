using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SpaceEngineers
{
    public class UpdatePanelWithTotals : Skeleton

    {

        //Assumes monospace font size 1
        const int COLUMN_WIDTH = 16;
        const string PANEL_NAME = "Ore Panel";
        const float ICE_THRESHOLD = 42000.0f;
        void Main(string argument)
        {
            string ERR_TXT = "";

            //get all tanks
            List<IMyTerminalBlock> v0 = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(v0, filterThis);
            if (v0.Count == 0)
            {
                ERR_TXT += "no Hydrogen Tank blocks found\n";
            }

            //get one LCD panel
            List<IMyTerminalBlock> l1 = new List<IMyTerminalBlock>();
            IMyTextPanel v1 = null;
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(l1, filterThis);
            if (l1.Count == 0)
            {
                ERR_TXT += "no LCD Panel blocks found\n";
            }
            else
            {
                for (int i = 0; i < l1.Count; i++)
                {
                    if (l1[i].CustomName.Contains(PANEL_NAME))
                    {
                        v1 = (IMyTextPanel)l1[i];
                        ((IMyTextPanel)v1).FontColor = new Color(255, 255, 255);

                        break;
                    }
                }
                if (v1 == null)
                {
                    ERR_TXT += "no LCD Panel block named " + PANEL_NAME + " found\n";
                }
            }
            //get all containers with inventories
            List<IMyTerminalBlock> inventoriedBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(inventoriedBlocks, filterInventories);
            if (inventoriedBlocks.Count == 0)
            {
                ERR_TXT += "No blocks with inventories found\n";
            }
            List<IMyInventory> inventories = new List<IMyInventory>();
            for (int i = 0; i < inventoriedBlocks.Count; i++)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventories.Add(inventoriedBlocks[i].GetInventory(0));
                if (inventoriedBlocks[i].InventoryCount == 2)
                {
                    inventories.Add(inventoriedBlocks[i].GetInventory(1));
                }
            }

            // display errors
            if (ERR_TXT != "")
            {
                Echo("Script Errors:\n" + ERR_TXT + "(make sure block ownership is set correctly)");
                return;
            }
            else { Echo(""); }

            // user variable declarations
            float percent = 0.0f;

            string outputText = "";
            // logic for tanks
            v0.Sort(compareBlocksByCustomName);
            Echo(v0.Count + " tanks found");
            for (int i = 0; i < v0.Count; i++)
            {
                percent = getExtraFieldFloat(v0[i], "Filled: (\\d+\\.?\\d*)%");
                string nameOut = v0[i].CustomName;
                outputText += "\n" + PadRight(nameOut + ": ", COLUMN_WIDTH) + percent + "%";
            }
            //Logic For inventories
            IDictionary<string, float> resourcesDict = new Dictionary<string, float>();
            //Always show ingots
            resourcesDict.Add("Cobalt Ingot", 0.0f);
            resourcesDict.Add("Gold Ingot", 0.0f);
            resourcesDict.Add("Iron Ingot", 0.0f);
            resourcesDict.Add("Magnesium Pow", 0.0f);
            resourcesDict.Add("Nickel Ingot", 0.0f);
            resourcesDict.Add("Platinum Ingot", 0.0f);
            resourcesDict.Add("Silicon Wafer", 0.0f);
            resourcesDict.Add("Silver Ingot", 0.0f);
            resourcesDict.Add("Uranium Ingot", 0.0f);
            //Always show ice
            resourcesDict.Add("Ice", 0.0f);

            Echo(inventories.Count + " inventories with ore");
            for (int i = 0; i < inventories.Count; i++)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventories[i].GetItems(items, filterItemsForOreAndIngots);
                for (int j = 0; j < items.Count; j++)
                {
                    String itemName = decodeItemName(items[j]);
                    float currentCount = getItemAmountAsFloat(items[j]);
                    float prevCount = 0.0f;
                    if (resourcesDict.TryGetValue(itemName, out prevCount))
                    {
                        currentCount += prevCount;
                        resourcesDict[itemName] = currentCount;

                    }
                }
            }
            foreach (KeyValuePair<string, float> kvp in resourcesDict)
            {
                string valueOut = kvp.Value < 5000 ? (float)(Math.Round((double)kvp.Value, 0)) + " " : (float)(Math.Round((double)kvp.Value / 1000, 0)) + "k";
                if (kvp.Key.Equals("Ice"))
                {
                    if (kvp.Value < ICE_THRESHOLD) ((IMyTextPanel)v1).FontColor = new Color(255, 0, 0);
                    valueOut = (float)(Math.Round((double)kvp.Value, 0)) + " ";
                }
                outputText += "\n" + PadRight(kvp.Key + ": ", COLUMN_WIDTH) + PadLeft(valueOut, 8);
            }


            ((IMyTextPanel)v1).WritePublicText(outputText, false);
            ((IMyTextPanel)v1).ShowPublicTextOnScreen();

        }


        float getItemAmountAsFloat(MyInventoryItem item)
        {
            float count = 0;
            float.TryParse("" + item.Amount, out count);
            return count;
        }

        string PadRight(string input, int num)
        {
            if (input.Length < num)
            {
                for (int i = input.Length; i < num; i++)
                {
                    input += " ";
                }
            }
            return input;
        }

        string PadLeft(string input, int num)
        {
            if (input.Length < num)
            {
                for (int i = input.Length; i < num; i++)
                {
                    input = " " + input;
                }
            }
            return input;
        }
        const string MULTIPLIERS = ".kMGTPEZY";
        float getExtraFieldFloat(IMyTerminalBlock block, string regexString)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(regexString, System.Text.RegularExpressions.RegexOptions.Singleline);
            float result = 0.0f;
            double parsedDouble;
            System.Text.RegularExpressions.Match match = regex.Match(block.DetailedInfo);
            if (match.Success)
            {
                if (Double.TryParse(match.Groups[1].Value, out parsedDouble))
                {
                    result = (float)parsedDouble;
                }
                if (MULTIPLIERS.IndexOf(match.Groups[2].Value) > -1)
                {
                    result = result * (float)Math.Pow(1000.0, MULTIPLIERS.IndexOf(match.Groups[2].Value));
                }
            }
            return result;
        }
        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
        }

        bool filterInventories(IMyTerminalBlock block)
        {
            if (block.CubeGrid != Me.CubeGrid || !block.HasInventory)
            {
                return false;
            }
            else
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                int allItemsCount = 0;
                block.GetInventory(0).GetItems(items);
                allItemsCount += items.Count;
                if (block.InventoryCount == 2)
                {
                    List<MyInventoryItem> items2 = new List<MyInventoryItem>();
                    block.GetInventory(1).GetItems(items2);
                    allItemsCount += items2.Count;
                }
                if (allItemsCount == 0)
                {
                    return false;
                }
            }
            return true;
        }

        private bool filterItemsForOreAndIngots(MyInventoryItem item)
        {
            return item.Type.TypeId.EndsWith("Ore") || item.Type.TypeId.EndsWith("Ingot");
        }

        private static int compareBlocksByCustomName(IMyTerminalBlock x, IMyTerminalBlock y)
        {
            return x.CustomName.CompareTo(y.CustomName);
        }

        String decodeItemName(MyInventoryItem item)
        {
            String TypeId = item.Type.TypeId;
            String SubtypeId = item.Type.SubtypeId;

            if (SubtypeId.Equals("Missile200mm")) { return "Missile Container"; }
            else if (SubtypeId.Equals("NATO_25x184mm")) { return "NATO Ammunition Container"; }
            else if (SubtypeId.Equals("NATO_5p56x45mm")) { return "NATO Magazine"; }
            else if (SubtypeId.Equals("HydrogenBottle")) { return "Hydrogen Bottle"; }
            else if (SubtypeId.Equals("OxygenBottle")) { return "Oxygen Bottle"; }
            else if (SubtypeId.Equals("AutomaticRifleItem")) { return "Automatic Rifle"; }
            else if (SubtypeId.Equals("PreciseAutomaticRifleItem")) { return "Precise Automatic Rifle"; }
            else if (SubtypeId.Equals("RapidFireAutomaticRifleItem")) { return "Rapid-Fire Automatic Rifle"; }
            else if (SubtypeId.Equals("UltimateAutomaticRifleItem")) { return "Elite Automatic Rifle"; }
            else if (SubtypeId.Equals("WelderItem")) { return "Welder"; }
            else if (SubtypeId.Equals("Welder2Item")) { return "Enhanced Welder"; }
            else if (SubtypeId.Equals("Welder3Item")) { return "Proficient Welder"; }
            else if (SubtypeId.Equals("Welder4Item")) { return "Elite Welder"; }
            else if (SubtypeId.Equals("AngleGrinderItem")) { return "Grinder"; }
            else if (SubtypeId.Equals("AngleGrinder2Item")) { return "Enhanced Grinder"; }
            else if (SubtypeId.Equals("AngleGrinder3Item")) { return "Proficient Grinder"; }
            else if (SubtypeId.Equals("AngleGrinder4Item")) { return "Elite Grinder"; }
            else if (SubtypeId.Equals("HandDrillItem")) { return "Hand Drill"; }
            else if (SubtypeId.Equals("HandDrill2Item")) { return "Enhanced Hand Drill"; }
            else if (SubtypeId.Equals("HandDrill3Item")) { return "Proficient Hand Drill"; }
            else if (SubtypeId.Equals("HandDrill4Item")) { return "Elite Hand Drill"; }
            else if (SubtypeId.Equals("BulletproofGlass")) { return "Bulletproof Glass"; }
            else if (SubtypeId.Equals("GravityGenerator")) { return "Gravity Generator Component"; }
            else if (SubtypeId.Equals("InteriorPlate")) { return "Interior Plate"; }
            else if (SubtypeId.Equals("LargeTube")) { return "Large Steel Tube"; }
            else if (SubtypeId.Equals("MetalGrid")) { return "Metal Grid"; }
            else if (SubtypeId.Equals("PowerCell")) { return "Power Cell"; }
            else if (SubtypeId.Equals("RadioCommunication")) { return "Radio Communication Component"; }
            else if (SubtypeId.Equals("SmallTube")) { return "Small Steel Tube"; }
            else if (SubtypeId.Equals("SolarCell")) { return "Solar Cell"; }
            else if (SubtypeId.Equals("SteelPlate")) { return "Steel Plate"; }
            else if (SubtypeId.Equals("Superconductor")) { return "Superconductor Conduits"; }
            else if (SubtypeId.Equals("Thrust")) { return "Thruster Component"; }
            else if (SubtypeId.Equals("Construction") || SubtypeId.Equals("Detector") || SubtypeId.Equals("Medical")
            || SubtypeId.Equals("Reactor")) { return SubtypeId + " Component"; }

            else if (TypeId.EndsWith("Ore"))
            {
                if (SubtypeId.Equals("Stone") || SubtypeId.Equals("Ice") || SubtypeId.Equals("Organic")) { return SubtypeId; }
                else if (SubtypeId.Equals("Scrap")) { return "Scrap Metal"; }
                else { return SubtypeId + " Ore"; }
            }

            else if (TypeId.EndsWith("Ingot"))
            {
                if (SubtypeId.Equals("Scrap")) { return "Old Scrap Metal"; }
                else if (SubtypeId.Equals("Stone")) { return "Gravel"; }
                else if (SubtypeId.Equals("Magnesium")) { return SubtypeId + " Pow"; }
                else if (SubtypeId.Equals("Silicon")) { return SubtypeId + " Wafer"; }
                else { return SubtypeId + " Ingot"; }
            }

            else { return SubtypeId; }
        }

    }
}
