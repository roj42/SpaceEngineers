using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace SpaceEngineers
{
    class AutoAlign : Skeleton
    {
        string REMOTE_CONTROL_NAME = ""; //Set name for remote control to orient on,
                                         //leave blank to use first one found
        double CTRL_COEFF = 0.8; //Set lower if overshooting, set higher to respond quicker
        int LIMIT_GYROS = 999; //Set to the max number of gyros to use
                               //(Using less gyros than you have allows you to still steer while
                               // leveler is operating.)

        ////////////////////////////////////////////////////////////

        IMyRemoteControl rc;
        List<IMyGyro> gyros;

        void Main(string argument)
        {
            if (rc == null)
                setup();

            //Get orientation from rc
            Matrix or;
            rc.Orientation.GetMatrix(out or);
            Vector3D down = or.Down;

            Vector3D grav = rc.GetNaturalGravity();
            grav.Normalize();

            for (int i = 0; i < gyros.Count; ++i)
            {
                var g = gyros[i];

                g.Orientation.GetMatrix(out or);
                var localDown = Vector3D.Transform(down, MatrixD.Transpose(or));

                var localGrav = Vector3D.Transform(grav, MatrixD.Transpose(g.WorldMatrix.GetOrientation()));

                //Since the gyro ui lies, we are not trying to control yaw,pitch,roll but rather we
                //need a rotation vector (axis around which to rotate)
                var rot = Vector3D.Cross(localDown, localGrav);
                double ang = rot.Length();
                ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang))); //More numerically stable than: ang=Math.Asin(ang)

                if (ang < 0.01)
                {   //Close enough
                    //Echo("Level");
                    g.SetValueBool("Override", false);
                    continue;
                }
                //Echo("Off level: "+(ang*180.0/3.14).ToString()+"deg");

                //Control speed to be proportional to distance (angle) we have left
                double ctrl_vel = g.GetMaximum<float>("Yaw") * (ang / Math.PI) * CTRL_COEFF;
                ctrl_vel = Math.Min(g.GetMaximum<float>("Yaw"), ctrl_vel);
                ctrl_vel = Math.Max(0.01, ctrl_vel); //Gyros don't work well at very low speeds
                rot.Normalize();
                rot *= ctrl_vel;
                g.SetValueFloat("Pitch", (float)rot.GetDim(0));
                g.SetValueFloat("Yaw", -(float)rot.GetDim(1));
                g.SetValueFloat("Roll", -(float)rot.GetDim(2));

                g.SetValueFloat("Power", 1.0f);
                g.SetValueBool("Override", true);
            }
        }

        void setup()
        {
            var l = new List<IMyTerminalBlock>();

            rc = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName(REMOTE_CONTROL_NAME);
            if (rc == null)
            {
                GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(l, x => x.CubeGrid == Me.CubeGrid);
                rc = (IMyRemoteControl)l[0];
            }

            GridTerminalSystem.GetBlocksOfType<IMyGyro>(l, x => x.CubeGrid == Me.CubeGrid);
            gyros = l.ConvertAll(x => (IMyGyro)x);
            if (gyros.Count > LIMIT_GYROS)
                gyros.RemoveRange(LIMIT_GYROS, gyros.Count - LIMIT_GYROS);
        }

    }
}
