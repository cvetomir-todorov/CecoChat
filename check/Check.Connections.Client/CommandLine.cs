using CommandLine;

namespace Check.Connections.Client;

public sealed class CommandLine
{
    [Option('a', "address", Required = true)]
    public string ServerAddress { get; set; }

    [Option('c', "clients", Required = true)]
    public int ClientCount { get; set; }

    [Option('m', "messages", Required = true)]
    public int MessageCount { get; set; }
}