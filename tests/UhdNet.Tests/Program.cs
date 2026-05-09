using UhdNet.Tests.Examples;

if (args.Length == 0)
{
    Console.WriteLine("Usage: UhdNet.Tests <example> [args...]");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  rx_samples_to_file   Stream RX samples from a USRP to a file.");
    Console.WriteLine("  device_info          Print info about the connected USRP(s).");
    return 0;
}

var name = args[0];
var rest = args[1..];

return name switch
{
    "rx_samples_to_file" => RxSamplesToFile.Run(rest),
    "device_info"        => DeviceInfo.Run(rest),
    _ => Fail(name),
};

static int Fail(string name)
{
    Console.Error.WriteLine($"Unknown example '{name}'.");
    return 2;
}
