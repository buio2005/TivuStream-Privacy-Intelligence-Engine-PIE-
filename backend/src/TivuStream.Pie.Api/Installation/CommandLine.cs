namespace TivuStream.Pie.Api.Installation;

/// <summary>
/// Separates the command a person typed from the settings passed with it.
/// </summary>
/// <remarks>
/// Settings travel on the command line as <c>--Name=value</c> or
/// <c>--Name value</c>, and the configuration reads them from there. A
/// command and its argument are what is left: the service is started with
/// its data folder as a setting, and the installation runs
/// <c>reset-password</c> with the same one.
/// </remarks>
internal static class CommandLine
{
    internal static string[] Positional(string[] args)
    {
        List<string> positional = [];

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            if (argument.StartsWith("--", StringComparison.Ordinal))
            {
                // A setting without '=' takes the next argument as its value.
                if (!argument.Contains('=', StringComparison.Ordinal))
                {
                    index++;
                }

                continue;
            }

            positional.Add(argument);
        }

        return [.. positional];
    }
}
