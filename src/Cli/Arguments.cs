namespace PulseGrid.Cli;

internal sealed class UsageException(string message) : Exception(message);

internal sealed class Arguments
{
    public string Command { get; }
    public Dictionary<string, string?> Options { get; } = new(StringComparer.Ordinal);
    public List<string> Positionals { get; } = [];
    public bool Json => Options.ContainsKey("json");

    public Arguments(string[] args)
    {
        if (args.Length > 64) throw new UsageException("Too many arguments.");
        Command = args.Length == 0 ? "help" : args[0];
        var allowed = Command switch
        {
            "capture" => new[] { "json", "source", "output", "exclude", "revision", "branch" },
            "inspect" or "validate" or "list" or "version" or "help" => ["json"],
            "export" => ["json", "output", "evidence"],
            "import" => ["json", "output"],
            _ => throw new UsageException("Unknown command. Run pulsegrid help.")
        };
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal)) { Positionals.Add(arg); continue; }
            var name = arg[2..];
            if (!allowed.Contains(name, StringComparer.Ordinal) || Options.ContainsKey(name))
                throw new UsageException("Unknown or repeated option: --" + name);
            string? value = null;
            if (name != "json")
            {
                if (++i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
                    throw new UsageException("Option --" + name + " requires a value.");
                value = args[i];
            }
            Options.Add(name, value);
        }
    }

    public string Required(string name) => Options.GetValueOrDefault(name)
        ?? throw new UsageException("Option --" + name + " is required.");

    public string OnePath(string? defaultPath = null)
    {
        if (Positionals.Count == 1) return Positionals[0];
        if (Positionals.Count == 0 && defaultPath is not null) return defaultPath;
        throw new UsageException("Specify exactly one input path.");
    }

    public void NoPositionals()
    {
        if (Positionals.Count != 0) throw new UsageException("This command does not accept positional arguments.");
    }
}
