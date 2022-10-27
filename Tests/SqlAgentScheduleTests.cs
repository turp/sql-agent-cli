using System;
using System.Collections.Generic;
using SqlAgent.Cli.Commands;
using Xunit;

namespace Tests;

public class SqlAgentScheduleTests
{
	[Fact]
	public void Monthly()
	{
		new B("every month at 09:15").Every(1, "month").OnDay(1).StartTime(91500).Validate();
		new B("every 3 months on 4 at 14:15").Every(3, "months").OnDay(4).StartTime(141500).Validate();
	}

	[Fact]
	public void Weekly()
	{
		new B("every week at 09:15").Every(1, "weeks").StartTime(91500).Validate();
		new B("every 3 weeks at 14:15").Every(3, "weeks").StartTime(141500).Validate();
		var s = new B("every week on mon,wed,fri at 14:15")
			.Every(1, "week")
			.On("mon", "wed", "fri")
			.StartTime(141500)
			.Validate();
		Assert.Equal(42, s.freq_interval);
	}

	[Fact]
	public void Daily()
	{
		new B("every day").Daily().StartTime(0).Validate();
		new B("every day at 00:00").Daily().StartTime(0).Validate("every day");
		new B("every day at 14:15").Daily().StartTime(141500).Validate();
		new B("at 14:15").Daily().StartTime(141500).Validate("every day at 14:15");
		new B("every 4 days at 14:15").Every(4, "days").StartTime(141500).Validate();
		new B("every 3 hours").Daily().Every(3, "hours").StartTime(0).Validate();
		new B("every 3 hours at 01:05").Daily().Every(3, "hours").StartTime(10500).Validate();
		new B("every 5 minutes from 2:05 to 19:09")
			.Daily()
			.Every(5, "minutes")
			.StartTime(20500)
			.EndTime(190900)
			.Validate("every 5 minutes from 02:05 to 19:09");
		new B("every 8 seconds from 2:05 to 19:09")
			.Daily()
			.Every(8, "seconds")
			.StartTime(20500)
			.EndTime(190900)
			.Validate("every 8 seconds from 02:05 to 19:09");
	}


	private class B
	{
		private readonly string _value;
		private readonly SqlAgentSchedule _actual;
		private readonly SqlAgentSchedule _expected = new();

		public B(string value)
		{
			_value = value;
			_actual = SqlAgentSchedule.Parse(value);
		}

		public B Daily()
		{
			return Every(1, "day");
		}

		public B StartTime(int value)
		{
			_expected.active_start_time = value;
			return this;
		}
		public B EndTime(int value)
		{
			_expected.active_end_time = value;
			return this;
		}
		public B Every(int number, string type)
		{
			if (type.ToLower().StartsWith("hour"))
			{
				_expected.freq_subday_type = 8;
				_expected.freq_subday_interval = number;
			}

			if (type.ToLower().StartsWith("min"))
			{
				_expected.freq_subday_type = 4;
				_expected.freq_subday_interval = number;
			}

			if (type.ToLower().StartsWith("sec"))
			{
				_expected.freq_subday_type = 2;
				_expected.freq_subday_interval = number;
			}

			if (type.ToLower().StartsWith("day"))
			{
				_expected.freq_type = 4;
				_expected.freq_interval = number;
			}

			if (type.ToLower().StartsWith("week"))
			{
				_expected.freq_type = 8;
				_expected.freq_recurrence_factor = number;
			}

			if (type.ToLower().StartsWith("month"))
			{
				_expected.freq_type = 16;
				_expected.freq_recurrence_factor = number;
			}

			return this;
		}
		public B On(params string[] values)
		{
			var days = new Dictionary<string, int>
			{
				{"sun", 1}
				, {"mon", 2}
				, {"tue", 4}
				, {"wed", 8}
				, {"thu", 16}
				, {"fri", 32}
				, {"sat", 64}
			};

			_expected.freq_interval = 0;
			foreach (var value in values)
			{
				if (!days.ContainsKey(value.ToLower())) continue;
				var day = days[value];
				_expected.freq_interval |= day;
			}

			return this;
		}
		public B OnDay(int value)
		{
			if (_expected.freq_type != 16)
				throw new Exception("OnDay is only valid for monthly schedule");

			_expected.freq_interval = value;
			return this;
		}
		public SqlAgentSchedule Validate(string e = "")
		{
			Assert.Equal(_expected.freq_type, _actual.freq_type);
			Assert.Equal(_expected.freq_interval, _actual.freq_interval);
			Assert.Equal(_expected.active_start_time, _actual.active_start_time);
			Assert.Equal(_expected.active_end_time, _actual.active_end_time);
			Assert.Equal(_expected.freq_recurrence_factor, _actual.freq_recurrence_factor);
			Assert.Equal(_expected.freq_subday_type, _actual.freq_subday_type);
			Assert.Equal(_expected.freq_subday_interval, _actual.freq_subday_interval);
			Assert.Equal(_expected.freq_relative_interval, _actual.freq_relative_interval);

			if (string.IsNullOrEmpty(e))
				e = _value;

			Assert.Equal(e, _actual.ToString());
			return _actual;
		}
	}
}