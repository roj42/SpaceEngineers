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
                    inventories.Add(inventoryBlocksOnGrid[i].GetInventory(0));
                    if (inventoryBlocksOnGrid[i].InventoryCount == 2)
                    {
                        inventories.Add(inventoryBlocksOnGrid[i].GetInventory(1));
                    }
                }
                Echo(inventories.Count + " inventories found");

                //Measure Volumes
                for (int i = 0; i < inventories.Count; i++)
                {
                    currentVolume += inventories[i].CurrentVolume;
                    maxVolume += inventories[i].MaxVolume;
                }
                Echo("Current Volume: " + currentVolume.ToString());
                Echo("Maximum Volume: " + maxVolume.ToString());

                //Try to parse the input
                int parseArg = 0;
                int.TryParse("" + argument, out parseArg);
                if (parseArg == 0)
                {
                    Echo("Argument must be an integer. Using default.");
                }
                VOLUME_THRESHOLD = parseArg;

                if (VOLUME_THRESHOLD > maxVolume)
                {
                    VOLUME_THRESHOLD = maxVolume;
                }
                //if no max volume set. Use 95% as default
                if (VOLUME_THRESHOLD.Equals(MyFixedPoint.Zero))
                {
                    VOLUME_THRESHOLD = MyFixedPoint.MultiplySafe(.95f, maxVolume);
                    Echo("Setting threshold to: " + VOLUME_THRESHOLD);

                }
                Echo("Threshold: " + VOLUME_THRESHOLD);


            }
            else ERR_TXT += "No light, inventories skipped\n";


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
                || type.EndsWith("Tank")  
                )
            {
                return false;
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
