using Dapper;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlAgent.Cli.Models;
using System.ComponentModel;

namespace SqlAgent.Cli.Commands;

[Description("List jobs from the target server")]
public class ListCommand : Command<ServerSettings>
{
	public override int Execute(CommandContext context, ServerSettings settings)
	{
		AnsiConsole.WriteLine($"List of jobs on server '{settings.Server}'");

		using var c = settings.GetConnection();
		var list = c.Query(_sql);

		var table = new Table().RoundedBorder();
		table.AddColumn("[grey]Name[/]");
		table.AddColumn("[grey]Enabled?[/]");
		table.AddColumn("[grey]Frequency[/]");
		table.AddColumn("[grey]Days[/]");
		table.AddColumn("[grey]Time[/]");

		foreach (var r in list)
		{
			var columns = new string[]
			{
				r.Name.ToString(),
				r.Enabled.ToString(),
				r.Frequency.ToString(),
				r.Days.ToString(),
				r.Time.ToString()
			};
			table.AddRow(columns);
		}

		AnsiConsole.Write(table);
		return 0;
	}

	private readonly string _sql = @$"
            -- https://www.mssqltips.com/sqlservertip/5019/sql-server-agent-job-schedule-reporting/

			-- manual jobs
            SELECT
	            j.name [Name]
	            , j.enabled [Enabled]
	            , '' as [Schedule Name]
	            , '' as freq_recurrence_factor
	            , 'Manual' [Frequency]
	            , '' as Days
	            , '' as [Time]
            FROM msdb.dbo.sysjobs j
	           left join msdb.dbo.sysjobschedules on j.job_id = sysjobschedules.job_id
	           left join msdb.dbo.sysschedules s on sysjobschedules.schedule_id = s.schedule_id
            WHERE s.freq_type IS NULL

			UNION

            -- jobs with daily schedule
            SELECT
	            j.name [Name]
	            , j.enabled [Enabled]
	            , s.name [Schedule Name]
	            , s.freq_recurrence_factor
	            , 'Daily' [Frequency]
	            , 'every ' + cast (freq_interval as varchar(3)) + ' day(s)'  Days
	            , CASE 
		            WHEN freq_subday_type = 2 THEN 'every ' + cast(freq_subday_interval as varchar(7)) + ' seconds' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            WHEN freq_subday_type = 4 THEN 'every ' + cast(freq_subday_interval as varchar(7)) + ' minutes' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            WHEN freq_subday_type = 8 THEN 'every ' + cast(freq_subday_interval as varchar(7)) + ' hours'   + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            ELSE 'starting at ' + stuff(stuff(RIGHT(replicate('0', 6) + cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
	            END [Time]
            FROM msdb.dbo.sysjobs j
	            inner join msdb.dbo.sysjobschedules on j.job_id = sysjobschedules.job_id
	            inner join msdb.dbo.sysschedules s on sysjobschedules.schedule_id = s.schedule_id
            WHERE freq_type = 4

            UNION

            SELECT
	            j.name [Name]
	            , j.enabled [Enabled]
	            , s.name [Schedule Name]
	            , s.freq_recurrence_factor
	            , 'Weekly' [Frequency]
	            , REPLACE (
		             CASE WHEN freq_interval&1 = 1 THEN 'Su, ' ELSE '' END
		            + CASE WHEN freq_interval&2 = 2 THEN 'M, ' ELSE '' END
		            + CASE WHEN freq_interval&4 = 4 THEN 'Tu, ' ELSE '' END
		            + CASE WHEN freq_interval&8 = 8 THEN 'W, ' ELSE '' END
		            + CASE WHEN freq_interval&16 = 16 THEN 'Th, ' ELSE '' END
		            + CASE WHEN freq_interval&32 = 32 THEN 'F, ' ELSE '' END
		            + CASE WHEN freq_interval&64 = 64 THEN 'Sa, ' ELSE '' END
		            , ', '
		            , ''
	            ) Days
	            , CASE
		            WHEN freq_subday_type = 2 then 'every ' + cast(freq_subday_interval as varchar(7)) + ' seconds' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':') 
		            WHEN freq_subday_type = 4 then 'every ' + cast(freq_subday_interval as varchar(7)) + ' minutes' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            WHEN freq_subday_type = 8 then 'every ' + cast(freq_subday_interval as varchar(7)) + ' hours'   + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            ELSE 'starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
	            END [Time]
            FROM msdb.dbo.sysjobs j
	            inner join msdb.dbo.sysjobschedules on j.job_id = sysjobschedules.job_id
	            inner join msdb.dbo.sysschedules s on sysjobschedules.schedule_id = s.schedule_id
            WHERE freq_type = 8

            UNION

            -- jobs with a monthly schedule
            select
	            j.name [Name]
	            , j.enabled [Enabled]
	            , s.name [Schedule Name]
	            , s.freq_recurrence_factor
	            , 'Monthly' [Frequency]
	            , case 
		            when freq_type = 32 then (
			            case 
				            when freq_relative_interval = 1 then 'First '
				            when freq_relative_interval = 2 then 'Second '
				            when freq_relative_interval = 4 then 'Third '
				            when freq_relative_interval = 8 then 'Fourth '
				            when freq_relative_interval = 16 then 'Last '
			            end 
			            + replace (
				            case when freq_interval = 1 THEN 'Sunday, ' ELSE '' END
				            + case when freq_interval = 2 THEN 'Monday, ' ELSE '' END
				            + case when freq_interval = 3 THEN 'Tuesday, ' ELSE '' END
				            + case when freq_interval = 4 THEN 'Wednesday, ' ELSE '' END
				            + case when freq_interval = 5 THEN 'Thursday, ' ELSE '' END
				            + case when freq_interval = 6 THEN 'Friday, ' ELSE '' END
				            + case when freq_interval = 7 THEN 'Saturday, ' ELSE '' END
				            + case when freq_interval = 8 THEN 'Day of Month, ' ELSE '' END
				            + case when freq_interval = 9 THEN 'Weekday, ' ELSE '' END
				            + case when freq_interval = 10 THEN 'Weekend day, ' ELSE '' END
			            ,', ' ,'' )
		            ) else cast(freq_interval as varchar(3)) END Days
	            , case
		            when freq_subday_type = 2 then 'every ' + cast(freq_subday_interval as varchar(7)) + ' seconds' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':') 
		            when freq_subday_type = 4 then 'every ' + cast(freq_subday_interval as varchar(7)) + ' minutes' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            when freq_subday_type = 8 then 'every ' + cast(freq_subday_interval as varchar(7)) + ' hours'   + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            else 'starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
	            end [Time]
            from msdb.dbo.sysjobs j
	            inner join msdb.dbo.sysjobschedules on j.job_id = sysjobschedules.job_id
	            inner join msdb.dbo.sysschedules s on sysjobschedules.schedule_id = s.schedule_id
            WHERE freq_type in (16, 32)
            ORDER BY j.name 
        ";
}