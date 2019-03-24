using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SpaceEngineers
{
    class VolumeWarningLight : Skeleton
    {

        const string IND_LIGHT_NAME = "Volume";
        Color WARNING_COLOR = new Color(255, 0, 0);
        MyFixedPoint VOLUME_THRESHOLD = MyFixedPoint.Zero;

        void Main(string argument)
        {
            string ERR_TXT = "";

            //Try to parse the input
            if (!String.IsNullOrEmpty(argument))
            {
                int parseArg = 0;
                int.TryParse("" + argument, out parseArg);
                VOLUME_THRESHOLD = parseArg;
            }
            Echo("Threshold: " + VOLUME_THRESHOLD);

            MyFixedPoint currentVolume = new MyFixedPoint();
            MyFixedPoint maxVolume = new MyFixedPoint();

            IMyLightingBlock volLight = null;
            //get LCD panels and clear them
            List<IMyTerminalBlock> lightsOnGrid = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(lightsOnGrid, filterThis);
            if (lightsOnGrid.Count == 0)
            {
                ERR_TXT += "no Light blocks found\n";
            }
            else
            {
                for (int i = 0; i < lightsOnGrid.Count; i++)
                {
                    if (lightsOnGrid[i].CustomName.Contains(IND_LIGHT_NAME))
                    {
                        volLight = (IMyLightingBlock)lightsOnGrid[i];
                        //Reset color for ice alarm
                        ((IMyLightingBlock)volLight).Color = new Color(255, 255, 255);
                    }
                }
                if (volLight == null && IND_LIGHT_NAME.Length > 0)
                {
                    ERR_TXT += "no Light named " + IND_LIGHT_NAME + "\n";
                }
            }

            //get all containers with inventories
            List<IMyTerminalBlock> inventoryBlocksOnGrid = new List<IMyTerminalBlock>();
            if (IND_LIGHT_NAME.Length > 0 && volLight != null)
            {
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
                //Echo(inventories.Count + " inventories with items in them");

                //Measure Volumes
                for (int i = 0; i < inventories.Count; i++)
                {
                    currentVolume += inventories[i].CurrentVolume;
                    maxVolume += inventories[i].MaxVolume;
                }
                Echo("Current Volume: " + currentVolume.ToString());
                Echo("Maximum Volume: " + maxVolume.ToString());
                //no max volume set. Use 90% as default
                if (VOLUME_THRESHOLD.Equals(MyFixedPoint.Zero))
                {
                    VOLUME_THRESHOLD = MyFixedPoint.MultiplySafe(.9f, maxVolume);
                    Echo("Setting threshold to: " + VOLUME_THRESHOLD);

                }

            }
            else ERR_TXT += "inventories skipped\n";


            // display collection errors
            if (ERR_TXT != "")
            {
                Echo("Script Errors:\n" + ERR_TXT + "(double check block ownership)");
                if (volLight == null)
                {
                    return;
                }
            }

            //display inventories
            if (IND_LIGHT_NAME.Length > 0 && volLight != null && thresholdReached(currentVolume, maxVolume))
            {
                volLight.Color = WARNING_COLOR;
                Echo("Threshold reached");
            }

            //END OF MAIN
        }

        bool thresholdReached(MyFixedPoint current, MyFixedPoint max)
        {
            if (current >= VOLUME_THRESHOLD)
            {
                return true;
            }
            return false;
        }

        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
        }

        bool filterInventories(IMyTerminalBlock block)
        {
            string type = getDetailedInfoValue(block, "Type");

            if (block.CubeGrid != Me.CubeGrid || !block.HasInventory
                || type.EndsWith("Engine") 
                || type.EndsWith("Reactor")
                || type.EndsWith("Generator")
                )
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

    }
}
