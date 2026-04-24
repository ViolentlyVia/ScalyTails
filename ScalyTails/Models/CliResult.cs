namespace ScalyTails.Models;

public record CliResult(string Stdout, string Stderr, int ExitCode)
{
    public bool Success => ExitCode == 0;
    public string Output => string.IsNullOrWhiteSpace(Stdout) ? Stderr : Stdout;
}
