using ConsoleAppFramework;
using Dot2Editor.Cli;

// Diagnostics must not pollute stdout, which carries the generated .editorconfig.
ConsoleApp.LogError = Console.Error.WriteLine;

ConsoleApp.Run(args, ConvertCommand.Execute);
