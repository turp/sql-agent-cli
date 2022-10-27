namespace SqlAgent.Cli.Commands;

public class Schedule
{
	public string Name { get; set; }
	public string Interval { get; set; }
	public string Target { get; set; }
	public bool Enabled { get; set; }
}