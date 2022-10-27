using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SqlAgent.Cli.Commands;

public class Job
{
	public string Name { get; set; }
	[YamlMember(ScalarStyle = ScalarStyle.Literal)]
	public string Description { get; set; }
	public string Category { get; set; }
	public string Owner { get; set; }
	public string NotifyEmailOperator { get; set; }
	public List<Step> Steps { get; set; } = new();
	public List<Schedule> Schedules { get; set; } = new();
}