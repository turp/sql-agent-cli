using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using Serilog;
using Spectre.Cli;

namespace SqlAgent.Cli.Models;

public class ServerSettings : CommandSettings
{
	private readonly ILogger _logger;

	[CommandOption("-s|--server <SERVER>")]
	[Description(
		@"Required. SQL Server instance (localhost | <server\instance, port> | server alias from appsetings).")]
	public string Server { get; set; }

	private readonly Dictionary<string, string> _serverLookup;
	public ServerSettings(ServerLookup lookup, ILogger logger)
	{
		_logger = logger;
		_serverLookup = lookup ?? new Dictionary<string, string>();
	}

	public override ValidationResult Validate()
	{
		if (string.IsNullOrEmpty(Server))
			return ValidationResult.Error("Server must be specified");

		if (_serverLookup.ContainsKey(this.Server.ToLower()))
			this.Server = _serverLookup[this.Server.ToLower()];

		return base.Validate();
	}

	public SqlConnection GetConnection()
	{
		return new SqlConnection($"Data Source={Server};Integrated Security=true;Pooling=false;");
	}
}