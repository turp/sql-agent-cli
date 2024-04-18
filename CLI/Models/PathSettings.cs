using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;

namespace SqlAgent.Cli.Models;

public class PathSettings(ServerLookup lookup, ILogger logger) : ServerSettings(lookup, logger)
{
	[CommandOption("-p|--path <PATH>")]
	[Description(@"Required. Directory path where job YAML files are stored.")]
	public string Path { get; set; }

	[CommandOption("-n|--name <NAME>")]
	[Description(@"Optional. Job name to import/export.")]
	public string Name { get; set; }

	[CommandOption("-t|--target <NAME>")]
	[Description(@"Optional. Environment target to allow for deploying different schedules based on server enviroment")]
	public string Target { get; set; }

	public override ValidationResult Validate()
	{
		if (string.IsNullOrEmpty(Path))
			return ValidationResult.Error("Path must be specified");

		if (!Directory.Exists(Path))
			Directory.CreateDirectory(Path);

		return base.Validate();
	}
}