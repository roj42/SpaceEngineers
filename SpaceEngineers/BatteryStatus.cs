using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;


namespace SpaceEngineers
{
    class BatteryStatus : Skeleton
    {

        /*     
         *  Original Written and tested by Stiggan and Malakeh in January 2015.     
         *     
         * Additions and fixes by Wicorel --January 2015     
         *     
         * Status display by request from forums Late Feb 2015 by Wicorel 
         */


        const string OurName = "Battery Status";


        void Log(string text)
        {

            var lcd = GridTerminalSystem.GetBlockWithName(OurName) as IMyTextPanel;
            if (lcd != null)
            {
                if (text.Equals("clear"))
                {
                    lcd.WritePublicText("");
                    lcd.WritePublicTitle(OurName + " Status");

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

        bool filterThis(IMyTerminalBlock block)
        {
            return block.CubeGrid == Me.CubeGrid;
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

        void Main()
        {
            Log("clear"); // make the status clear for this run   

            List<IMyTerminalBlock> batteries = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, filterThis);

            int col1Width = 16;
            int col2Width = 16;
            string header = PadRight("Name", col1Width) + PadRight("Charge", col2Width) + "Status";
            Log(header);

            for (int i = 0; i < batteries.Count; i++)
            {
                int charge = 0;
                int capacity = 0;
                int percentthisbattery = 0;

                capacity += getPowerAsInt(getDetailedInfoValue(batteries[i], "Max Stored Power"));
                charge += getPowerAsInt(getDetailedInfoValue(batteries[i], "Stored power"));
                if (capacity > 0)
                {

                    percentthisbattery = (charge * 100) / capacity;
                    string statustext = "";
                    statustext = PadRight(batteries[i].CustomName, col1Width);
                    statustext += PadRight(percentthisbattery+"", col2Width);
                    if (isRecharging(batteries[i]))
                    {
                        statustext += "Recharging";
                    }
                    else
                    {
                        statustext += "Discharging";
                    }

                    Log(statustext);
                }
            }
        }


    }
}
