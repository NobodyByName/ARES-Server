using Ares.Common;
using Ares.Common.CommandLine;
using Ares.MapData;
using Ares.Structs.Core;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine(ExtractCommand.Usage);
    return args.Length == 0 ? ExtractCommand.UsageError : ExtractCommand.Success;
}

if (args[0] == "--version")
{
    Console.WriteLine($"ARES-MapData {AresVersion.Current} (game build {GameBuild.SteamBuildId})");
    return ExtractCommand.Success;
}

if (args[0] != ExtractCommand.Name)
{
    Console.Error.WriteLine(ExtractCommand.Usage);
    Console.Error.WriteLine($"ARES-MapData: error: unknown command '{args[0]}'");
    return ExtractCommand.UsageError;
}

ExtractCommand command;
try
{
    command = ExtractCommand.Parse(args[1..]);
}
catch (HelpRequestedException)
{
    Console.WriteLine(ExtractCommand.Usage);
    return ExtractCommand.Success;
}
catch (CommandLineException error)
{
    Console.Error.WriteLine(ExtractCommand.Usage);
    Console.Error.WriteLine($"ARES-MapData: error: {error.Message}");
    return ExtractCommand.UsageError;
}

return command.Run(Console.Out, Console.Error);
