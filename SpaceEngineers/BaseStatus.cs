﻿using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SpaceEngineers
{
    class BaseStatus : Skeleton
    {

        const string TANK_PANEL_NAME = "LCD Panel Tanks";
        const string ENGINE_PANEL_NAME = "LCD Panel Engines";
        const string BATTERY_PANEL_NAME = "LCD Panel Batteries";

        const string ORE_PANEL_NAME = "LCD Panel Ore";
        const float ICE_THRESHOLD = 50000.0f;
        //Assumes monospace font size 1
        const int COLUMN_WIDTH = 16;


        void Main()
        {
            string ERR_TXT = "";

            IMyTextPanel orePanel = null;
            IMyTextPanel tankPanel = null;
            IMyTextPanel enginePanel = null;
            IMyTextPanel batteryPanel = null;
            //get LCD panels and clear them
            List<IMyTerminalBlock> lcdPanelsOnGrid = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcdPanelsOnGrid, filterThis);
            if (lcdPanelsOnGrid.Count == 0)
            {
                ERR_TXT += "no LCD Panel blocks found\n";
            }
            else
            {
                for (int i = 0; i < lcdPanelsOnGrid.Count; i++)
                {
                    switch (lcdPanelsOnGrid[i].CustomName)
                    {
                        case ORE_PANEL_NAME:
                            orePanel = (IMyTextPanel)lcdPanelsOnGrid[i];
                            //Reset color for ice alarm
                            ((IMyTextPanel)orePanel).FontColor = new Color(255, 255, 255);
                            Log("clear", orePanel);
                            continue;
                        case TANK_PANEL_NAME:
                            tankPanel = (IMyTextPanel)lcdPanelsOnGrid[i];
                            Log("clear", tankPanel);
                            continue;
                        case ENGINE_PANEL_NAME:
                            enginePanel = (IMyTextPanel)lcdPanelsOnGrid[i];
                            Log("clear", enginePanel);
                            continue;
                        case BATTERY_PANEL_NAME:
                            batteryPanel = (IMyTextPanel)lcdPanelsOnGrid[i];
                            Log("clear", batteryPanel);
                            continue;
                        default:
                            continue;
                    }
                }
                if (orePanel == null && ORE_PANEL_NAME.Length > 0)
                {
                    ERR_TXT += "no LCD Panel block named " + ORE_PANEL_NAME + " found\n";
                }

                if (tankPanel == null && TANK_PANEL_NAME.Length > 0)
                {
                    ERR_TXT += "no LCD Panel block named " + TANK_PANEL_NAME + " found\n";
                }
                if (enginePanel == null && ENGINE_PANEL_NAME.Length > 0)
                {
                    ERR_TXT += "no LCD Panel block named " + ENGINE_PANEL_NAME + " found\n";
                }
                if (batteryPanel == null && BATTERY_PANEL_NAME.Length > 0)
                {
                    ERR_TXT += "no LCD Panel block named " + BATTERY_PANEL_NAME + " found\n";
                }
            }

            //get all containers with inventories
            if (ORE_PANEL_NAME.Length > 0)
            {
                List<IMyTerminalBlock> inventoryBlocksOnGrid = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(inventoryBlocksOnGrid, filterInventories);
                if (inventoryBlocksOnGrid.Count == 0)
                {
                    ERR_TXT += "No blocks with inventories found\n";
                }
                List<IMyInventory> inventories = new List<IMyInventory>();
                for (int i = 0; i < inventoryBlocksOnGrid.Count; i++)
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    inventories.Add(inventoryBlocksOnGrid[i].GetInventory(0));
                    if (inventoryBlocksOnGrid[i].InventoryCount == 2)
                    {
                        inventories.Add(inventoryBlocksOnGrid[i].GetInventory(1));
                    }
                }
            }

            //get all tanks
            if (TANK_PANEL_NAME.Length > 0)
            {

                List<IMyTerminalBlock> tanksOnGrid = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanksOnGrid, filterThis);
                if (tanksOnGrid.Count == 0)
                {
                    ERR_TXT += "no gas tank blocks found\n";
                }
            }

            //Get all batteries
            if(BATTERY_PANEL_NAME.Length > 0)
            List<IMyTerminalBlock> batteriesOnGrid = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteriesOnGrid, filterThis);
            if (batteriesOnGrid.Count == 0)
            {
                ERR_TXT += "no battery blocks found\n";
            }

            // display errors
            if (ERR_TXT != "")
            {
                Echo("Script Errors:\n" + ERR_TXT + "(make sure block ownership is set correctly)");
                return;
            }


            //Batteries!
            string header = PadRight("Name", COLUMN_WIDTH) + PadRight("Charge", COLUMN_WIDTH) + "Status";
            Log(header,batteryPanel);

            for (int i = 0; i < batteriesOnGrid.Count; i++)
            {
                int charge = 0;
                int capacity = 0;
                int percentthisbattery = 0;

                capacity += getPowerAsInt(getDetailedInfoValue(batteriesOnGrid[i], "Max Stored Power"));
                charge += getPowerAsInt(getDetailedInfoValue(batteriesOnGrid[i], "Stored power"));
                if (capacity > 0)
                {

                    percentthisbattery = (charge * 100) / capacity;
                    string statustext = "";
                    statustext = PadRight(batteriesOnGrid[i].CustomName, COLUMN_WIDTH);
                    statustext += PadRight(percentthisbattery + "", COLUMN_WIDTH);
                    if (isRecharging(batteriesOnGrid[i]))
                    {
                        statustext += "Recharging";
                    }
                    else
                    {
                        statustext += "Discharging";
                    }

                    Log(statustext,batteryPanel);
                }
            }

        //END OF MAIN
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

        void Log(string text, IMyTextPanel lcd)
        {

            if (lcd != null)
            {
                if (text.Equals("clear"))
                {
                    lcd.WritePublicText("");
                    lcd.WritePublicTitle(lcd.CustomName + " Status");

                }
                else
                {
                    string oldtext = lcd.GetPublicText();

                    lcd.ShowTextureOnScreen();
                    lcd.WritePublicText(oldtext + "\n" + text, false);
                }
                lcd.ShowPublicTextOnScreen();
            }

        }

        string getDetailedInfoValue(IMyTerminalBlock block, string name)
        {
            string value = "";
            string[] lines = block.DetailedInfo.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(':');
                if (line[0].Equals(name))
                {
                    value = line[1].Substring(1);
                    break;
                }
            }
            return value;
        }

        bool detailExist(IMyTerminalBlock block, string name)
        {
            return !String.IsNullOrEmpty(getDetailedInfoValue(block, name));
        }

        int getPowerAsInt(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return 0;
            }
            string[] values = text.Split(' ');
            if (values[1].Equals("kW"))
            {
                return (int)(float.Parse(values[0]) * 1000f);
            }
            else if (values[1].Equals("kWh"))
            {
                return (int)(float.Parse(values[0]) * 1000f);
            }
            else if (values[1].Equals("MW"))
            {
                return (int)(float.Parse(values[0]) * 1000000f);
            }
            else if (values[1].Equals("MWh"))
            {
                return (int)(float.Parse(values[0]) * 1000000f);
            }
            else
            {
                return (int)float.Parse(values[0]);
            }
        }

        bool isHealthy(IMyTerminalBlock block)
        {
            return (block.IsFunctional && block.IsWorking);
        }



        int getPower(IMyTerminalBlock block, bool max)
        {
            if (max)
            {
                if (!block.IsBeingHacked)
                {
                    return getPowerAsInt(getDetailedInfoValue(block, "Max Output"));
                }
            }
            else
            {
                return getPowerAsInt(getDetailedInfoValue(block, "Current Output"));
            }
            return 0;
        }


        bool isRecharging(IMyTerminalBlock block)
        {
            return detailExist(block, "Fully recharged in");
        }


    }
}
