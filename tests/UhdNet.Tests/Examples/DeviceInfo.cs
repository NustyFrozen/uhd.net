using System;
using UhdNet;

namespace UhdNet.Tests.Examples;

internal static class DeviceInfo
{
    public static int Run(string[] args)
    {
        string deviceArgs = args.Length > 0 ? args[0] : string.Empty;

        Console.WriteLine($"UHD version: {Uhd.VersionString}");
        Console.WriteLine($"UHD ABI:     {Uhd.AbiString}");
        Console.WriteLine();

        Console.WriteLine($"Discovering USRPs (args=\"{deviceArgs}\")...");
        try
        {
            using var found = Usrp.Find(deviceArgs);
            Console.WriteLine($"Found {found.Count} device(s):");
            for (int i = 0; i < found.Count; i++)
            {
                Console.WriteLine($"  [{i}] {found[i]}");
            }

            if (found.Count == 0)
            {
                Console.WriteLine("No devices found. (Is one connected and powered on?)");
                return 0;
            }

            using var usrp = Usrp.Make(deviceArgs);
            Console.WriteLine();
            Console.WriteLine("Selected device:");
            Console.WriteLine(usrp.GetPrettyString());

            nuint nMb = usrp.NumberOfMotherboards;
            for (nuint m = 0; m < nMb; m++)
            {
                Console.WriteLine($"Motherboard[{m}]: {usrp.GetMotherboardName(m)}, " +
                                  $"clock={usrp.GetMasterClockRate(m) / 1e6:F3} MHz");
            }

            nuint nRx = usrp.NumberOfRxChannels;
            for (nuint c = 0; c < nRx; c++)
            {
                Console.WriteLine($"RX[{c}]: subdev={usrp.GetRxSubdevName(c)}, " +
                                  $"antenna={usrp.GetRxAntenna(c)}, " +
                                  $"freq={usrp.GetRxFrequency(c) / 1e6:F3} MHz, " +
                                  $"rate={usrp.GetRxRate(c) / 1e6:F3} Msps, " +
                                  $"gain={usrp.GetRxGain(c):F1} dB");
            }
            return 0;
        }
        catch (UhdException ex)
        {
            Console.Error.WriteLine($"UHD error: {ex.Code} - {ex.Message}");
            return 3;
        }
    }
}
