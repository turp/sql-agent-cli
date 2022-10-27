using Microsoft.Extensions.Configuration;
using Spectre.Cli;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog;
using Serilog.Events;
using SqlAgent.Cli.Commands;
using SqlAgent.Cli.Models;

namespace SqlAgent.Cli;

class Program
{
	static int Main(string[] args)
	{
		var app = new CommandApp(ConfigureServices());

		app.Configure(config =>
		{
			config.Settings.ApplicationName = "SQL Agent Job Manager";
			config.AddCommand<ImportCommand>("import");
			config.AddCommand<ExportCommand>("export");
			config.AddCommand<ListCommand>("list");
		});

		return app.Run(args);
	}

	public static ITypeRegistrar ConfigureServices()
	{
		var collection = new ServiceCollection();

		collection.AddSingleton<ServerLookup>(p =>
		{
			IConfiguration config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables()
				.Build();

			var servers = config.GetRequiredSection("servers").Get<ServerLookup>();

			return servers;
		});

		collection.AddSingleton<ServerSettings>();
		collection.AddSingleton<PathSettings>();
		collection.AddSingleton<ILogger>(p => CreateLogger());

		return new TypeRegistrar(collection);
	}

	private static Logger CreateLogger()
	{
		var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
		return new LoggerConfiguration()
			.MinimumLevel.ControlledBy(levelSwitch)
			.WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
	}
}