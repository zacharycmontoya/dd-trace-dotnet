using System;
using FIXForge.NET.FIX;
using FIXForge.NET.FIX.Fix2FixmlConverter;

namespace Assert.OnixS.FIX
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("init engine");
                var engineSettings = new EngineSettings
                {
                    LicenseFile = "OnixS.FixEngineNet.Trial.lic"
                };
                Engine.Init(engineSettings);

                Console.WriteLine("create converter");
                var converter = new FixmlConverter(ProtocolVersion.FIX42);
                Console.WriteLine("after");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
