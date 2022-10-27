using System.IO;
using System.Linq;
using Dapper;
using Spectre.Console;
using Spectre.Cli;
using SqlAgent.Cli.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.ComponentModel;

namespace SqlAgent.Cli.Commands;

[Description("Export jobs from server")]
public class ExportCommand : Command<PathSettings>
{
	public override int Execute(CommandContext context, PathSettings settings)
	{
		AnsiConsole.WriteLine($"Exporting jobs from server {settings.Server} to {Path.GetFullPath(settings.Path)}");

		using var c = settings.GetConnection();
		var result = c.QueryMultiple(_sqlQuery);

		var jobs = result.Read();
		var steps = result.Read();
		var schedules = result.Read();

		foreach (var j in jobs)
		{
			var job = new Job
			{
				Name = j.Name
				, Category = j.Category
				, Description = j.Description
				, Owner = j.Owner
				, NotifyEmailOperator = j.NotifyEmailOperator
			};

			AnsiConsole.WriteLine($"* {j.Name}");

			foreach (var o in steps.Where(s => s.job_id == j.job_id).OrderBy(s => s.step_id))
			{
				job.Steps.Add(new Step
				{
					Name = o.name
					, Command = o.command
					, Subsystem = o.subsystem
					, Database = o.database
					, Proxy = o.proxy
				});
			}

			foreach (var o in schedules.Where(s => s.job_id == j.job_id))
			{
				var t = new SqlAgentSchedule
				{
					freq_interval = o.freq_interval
					, freq_type = o.freq_type
					, freq_recurrence_factor = o.freq_recurrence_factor
					, freq_subday_type = o.freq_subday_type
					, freq_subday_interval = o.freq_subday_interval
					, freq_relative_interval = o.freq_relative_interval
					, active_start_time = o.active_start_time
					, active_end_time = o.active_end_time
				};
				job.Schedules.Add(new Schedule
				{
					Name = o.name, 
					Enabled = o.enabled == 1, 
					Interval = t.ToString(),
					Target = settings.Target
				});
			}

			Save(job, settings);
		}

		return 0;
	}

	private void Save(Job job, PathSettings settings)
	{
		var path = Path.Combine(settings.Path, $"{job.Name}.yml").Replace('?', '-');
		File.Delete(path);

		var yaml = new SerializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();

		var text = yaml.Serialize(job);
		File.AppendAllText(path, text);
	}

	private string _sqlQuery = $@"
            SELECT
                j.job_id
	            , j.name [Name]
	            , CASE WHEN j.description LIKE 'No description available%' THEN NULL ELSE j.description END Description
	            , CASE WHEN c.name LIKE '%Uncategorized%' THEN NULL ELSE c.name END [Category]
	            , l.name as [Owner]
	            , o.name as NotifyEmailOperator
            FROM msdb.dbo.sysjobs j
	            left join msdb.dbo.syscategories c ON j.category_id = c.category_id
	            join master..syslogins l on j.owner_sid = l.sid
	            LEFT JOIN [msdb].[dbo].[sysoperators] o on j.notify_email_operator_id = o.id
            WHERE j.enabled = 1
            ORDER BY j.name;

            -- steps
            SELECT s.[job_id]
                , s.[step_id]
                , s.[step_name] as name
                , s.[subsystem]
                , s.[command]
                , s.[database_name] as [database]
                , p.name as proxy
            FROM [msdb].[dbo].[sysjobsteps] s
                LEFT JOIN msdb..sysproxies p ON s.proxy_id = p.proxy_id;

            -- schedules
            select j.job_id
				, j.name as JobName
	            , s.name
                , s.enabled
				, freq_type
				, freq_interval
				, freq_recurrence_factor
				, freq_relative_interval
				, freq_subday_type
				, freq_subday_interval
				, active_start_time
                , active_end_time
	            , case
		            when freq_type = 4 then 
			            CASE WHEN freq_interval = 1 THEN 'every day' ELSE 'every ' + cast(freq_interval as varchar(7)) + ' days' END
		            when freq_type = 8 then 
			            CASE WHEN freq_recurrence_factor = 1 THEN 'every week' ELSE 'every ' + cast(freq_recurrence_factor as varchar(7)) + ' weeks' END
		            else 
			            CASE WHEN freq_recurrence_factor = 1 THEN 'every month' ELSE 'every ' + cast(freq_recurrence_factor as varchar(7)) + ' months' END
	            end frequency
	            , case 
		            when freq_type = 4 then NULL
		            when freq_type = 8 then (
			            CASE WHEN freq_interval&1 = 1 THEN 'sun ' ELSE '' END
			            + CASE WHEN freq_interval&2 = 2 THEN 'mon ' ELSE '' END
			            + CASE WHEN freq_interval&4 = 4 THEN 'tue ' ELSE '' END
			            + CASE WHEN freq_interval&8 = 8 THEN 'wed ' ELSE '' END
			            + CASE WHEN freq_interval&16 = 16 THEN 'thu ' ELSE '' END
			            + CASE WHEN freq_interval&32 = 32 THEN 'fri ' ELSE '' END
			            + CASE WHEN freq_interval&64 = 64 THEN 'sat ' ELSE '' END 
		            )
		            when freq_type = 32 then (
			            case
				            when freq_relative_interval = 1 then 'first '
				            when freq_relative_interval = 2 then 'second '
				            when freq_relative_interval = 4 then 'third '
				            when freq_relative_interval = 8 then 'fourth '
				            when freq_relative_interval = 16 then 'last '
			            end +
			            case when freq_interval = 1 THEN 'sun ' ELSE '' END
				            + case when freq_interval = 2 THEN 'mon ' ELSE '' END
				            + case when freq_interval = 3 THEN 'tue ' ELSE '' END
				            + case when freq_interval = 4 THEN 'wed ' ELSE '' END
				            + case when freq_interval = 5 THEN 'thu ' ELSE '' END
				            + case when freq_interval = 6 THEN 'fri ' ELSE '' END
				            + case when freq_interval = 7 THEN 'sat ' ELSE '' END
				            + case when freq_interval = 8 THEN 'Day of Month ' ELSE '' END
				            + case when freq_interval = 9 THEN 'Weekday ' ELSE '' END
				            + case when freq_interval = 10 THEN 'Weekend day ' ELSE '' END
		            )
		            else 'day ' + cast(freq_interval as varchar(3)) + ' of the month' END [days]
	            , case
		            when freq_subday_type = 2 then ' every ' + cast(freq_subday_interval as varchar(7)) + ' seconds' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':') 
		            when freq_subday_type = 4 then ' every ' + cast(freq_subday_interval as varchar(7)) + ' minutes' + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            when freq_subday_type = 8 then ' every ' + cast(freq_subday_interval as varchar(7)) + ' hours'   + ' starting at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
		            else ' at ' + stuff(stuff(RIGHT(replicate('0', 6) +  cast(active_start_time as varchar(6)), 6), 3, 0, ':'), 6, 0, ':')
	            end time
            from msdb.dbo.sysjobs j
	            inner join msdb.dbo.sysjobschedules js on j.job_id = js.job_id
	            inner join msdb.dbo.sysschedules s on js.schedule_id = s.schedule_id
            WHERE j.enabled = 1
			--AND s.enabled = 1
			ORDER BY j.name, s.name
        ";
}