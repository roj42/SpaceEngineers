using System;

using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI;

namespace SpaceEngineers
{
    public abstract class Skeleton : IMyGridProgram
    {
        public Func<IMyIntergridCommunicationSystem> IGC_ContextGetter { set => throw new NotImplementedException(); }
        public Sandbox.ModAPI.Ingame.IMyGridTerminalSystem GridTerminalSystem { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Sandbox.ModAPI.Ingame.IMyProgrammableBlock Me { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSpan ElapsedTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Storage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMyGridProgramRuntimeInfo Runtime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Action<string> Echo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool HasMainMethod => throw new NotImplementedException();

        public bool HasSaveMethod => throw new NotImplementedException();

        public void Main(string argument)
        {
            throw new NotImplementedException();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}