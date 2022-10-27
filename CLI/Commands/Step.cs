using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SqlAgent.Cli.Commands;

public class Step
{
	public string Name { get; set; }
	public string Subsystem { get; set; }
	[YamlMember(ScalarStyle = ScalarStyle.Literal)]
	public string Command { get; set; }
	public string Database { get; set; }
	public string Proxy { get; set; }
}