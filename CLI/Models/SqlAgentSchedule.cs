using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlAgent.Cli.Models;

public class SqlAgentSchedule
{
	public int freq_type { get; set; } = 4;
	public int freq_interval { get; set; } = 1;
	public int freq_recurrence_factor { get; set; }
	public int freq_relative_interval { get; set; }
	public int freq_subday_type { get; set; }
	public int freq_subday_interval { get; set; }
	public int active_start_time { get; set; }
	public int active_end_time { get; set; } = 235959;

	public override string ToString()
	{
		var result = string.Empty;
		var time_period = new Func<int, string>((v) =>
		{
			if (v == 8) return "hours";
			if (v == 4) return "minutes";
			if (v == 2) return "seconds";
			return string.Empty;
		});

		if (freq_type == 4)
		{
			if (freq_interval == 1)
			{
				if (freq_subday_interval > 0)
					result += $" every {freq_subday_interval} {time_period(freq_subday_type)}";
				else
					result += "every day";
			}
			else
				result += $"every {freq_interval} days";
		}
		if (freq_type == 8)
		{
			result = freq_recurrence_factor > 1 ? $"every {freq_recurrence_factor} weeks" : "every week";
		}
		if (freq_type == 16)
		{
			result = freq_recurrence_factor > 1 ? $"every {freq_recurrence_factor} months" : "every month";
		}

		if (freq_type == 8 && freq_interval > 1)
		{
			var days = new List<string>();
			foreach (var day in Days)
			{
				if ((freq_interval & day.Value) == day.Value)
					days.Add(day.Key);
			}
			result += " on " + string.Join(',', days);
		}

		if (freq_type == 16 && freq_interval > 1)
		{
			result += $" on {freq_interval}";
		}

		var time = new Func<int, string>((v) => $"{((v - (v % 10000)) / 10000):00}:{(v % 10000) / 100:00}");

		if (active_start_time > 0 && active_end_time == 235959)
			result += $" at {time(active_start_time)}";

		if (active_end_time != 235959)
			result += $@" from {time(active_start_time)} to {time(active_end_time)}";

		return result.Trim();
	}

	private static readonly Dictionary<string, int> Days = new()
	{
		{"sun", 1}
		, {"mon", 2}
		, {"tue", 4}
		, {"wed", 8}
		, {"thu", 16}
		, {"fri", 32}
		, {"sat", 64}
	};

	public static SqlAgentSchedule Parse(string value)
	{
		var result = new SqlAgentSchedule();

		var match = Regex.Match(value, @"on\s+([a-zA-Z,]+)");
		if (match.Success)
		{
			result.freq_interval = 0;
			var values = match.Groups[1].Value.Split(",");
			foreach (var v in values)
			{
				if (!Days.ContainsKey(v.ToLower())) continue;
				var day = Days[v];
				result.freq_interval |= day;
			}
		}

		// monthly day
		match = Regex.Match(value, @"on\s+(\d+)");
		if (match.Success)
			result.freq_interval = int.Parse(match.Groups[1].Value);

		match = Regex.Match(value, @"^every\s(\d+)?\s?([a-zA-Z]+)");
		if (match.Success)
		{
			var num = (string.IsNullOrEmpty(match.Groups[1].Value)) ? 1 : int.Parse(match.Groups[1].Value);
			var type = match.Groups[2].Value;

			if (type.ToLower().StartsWith("hour"))
			{
				result.freq_subday_type = 8;
				result.freq_subday_interval = num;
			}

			if (type.ToLower().StartsWith("min"))
			{
				result.freq_subday_type = 4;
				result.freq_subday_interval = num;
			}

			if (type.ToLower().StartsWith("sec"))
			{
				result.freq_subday_type = 2;
				result.freq_subday_interval = num;
			}

			if (type.ToLower().StartsWith("day"))
			{
				result.freq_type = 4;
				result.freq_interval = num;
			}

			if (type.ToLower().StartsWith("week"))
			{
				result.freq_type = 8;
				result.freq_recurrence_factor = num;
			}

			if (type.ToLower().StartsWith("month"))
			{
				result.freq_type = 16;
				result.freq_recurrence_factor = num;
			}
		}

		match = Regex.Match(value, @"[at|from] (\d+:\d+)");
		if (match.Success)
			result.active_start_time = int.Parse(match.Groups[1].Value.Replace(":", "")) * 100;

		match = Regex.Match(value, @"to (\d+:\d+)");
		if (match.Success)
			result.active_end_time = int.Parse(match.Groups[1].Value.Replace(":", "")) * 100;

		return result;
	}
}