using Dapper;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlAgent.Cli.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SqlAgent.Cli.Commands;

[Description("Import jobs to server")]
public class ImportCommand : Command<PathSettings>
{
	public override int Execute(CommandContext context, PathSettings settings)
	{
		AnsiConsole.WriteLine($"Import jobs to server '{settings.Server}' from '{Path.GetFullPath(settings.Path)}'");

		using var connection = settings.GetConnection();
		foreach (var job in Get(settings))
		{
			AnsiConsole.WriteLine($"\nImporting '{job.Name}'");

			CreateCategory(connection, job.Category);
			DeleteJob(connection, job.Name);

			var jobId = CreateJob(connection, job);
			CreateJobSteps(connection, job, jobId);
			CreateJobSchedule(connection, job, jobId, settings.Target);
		}

		return 0;
	}

	private void CreateJobSchedule(SqlConnection c, Job job, Guid jobId, string target)
	{
		AnsiConsole.WriteLine($"Creating Schedules");
		foreach (var s in job.Schedules)
		{
			target = string.IsNullOrEmpty(target) ? string.Empty : target;
			if (!target.Equals(s.Target ?? "", StringComparison.CurrentCultureIgnoreCase))
			{
				AnsiConsole.WriteLine($"* '{s.Name}' skipped. Schedule target '{s.Target}' does not match target '{target}'");
				continue;
			}

			AnsiConsole.WriteLine($"* {s.Name}");

			var schedule = SqlAgentSchedule.Parse(s.Interval);
			var sql = $@"
                    EXEC msdb.dbo.sp_add_jobschedule 
                        @job_id='{jobId}'
                        , @name='{s.Name}'
                        , @enabled = {(s.Enabled ? 1 : 0)}
                        , @freq_type={schedule.freq_type}
                        , @freq_interval={schedule.freq_interval}
                        , @freq_subday_type={schedule.freq_subday_type}
                        , @freq_subday_interval={schedule.freq_subday_interval}
                        , @freq_relative_interval={schedule.freq_relative_interval}
                        , @freq_recurrence_factor={schedule.freq_recurrence_factor}
                        , @active_start_time={schedule.active_start_time}
                        , @active_end_time={schedule.active_end_time} 
                  ";

			c.Execute(sql);
		}
	}

	private void CreateJobSteps(SqlConnection c, Job job, Guid jobId)
	{
		AnsiConsole.WriteLine($"Creating Steps: ");
		const int quitJobReportingSuccess = 1;
		const int moveToNextStep = 3;

		for (var i = 0; i < job.Steps.Count; i++)
		{
			var step = job.Steps[i];
			AnsiConsole.WriteLine($"* {step.Name}");

			var onSuccessAction = (i == job.Steps.Count - 1) ? quitJobReportingSuccess : moveToNextStep;
			var sql = $@"
                    EXEC msdb.dbo.sp_add_jobstep 
                        @job_id='{jobId}'
                        , @step_name='{step.Name}'
                        , @subsystem='{step.Subsystem}'
                        , @command='{step.Command.Replace("'", "''").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n")}'
                  		, @database_name = '{step.Database}'
                        , @proxy_name='{step.Proxy}'
		                , @on_success_action={onSuccessAction}
                  ";

			c.Execute(sql);
		}
	}

	private Guid CreateJob(SqlConnection c, Job job)
	{
		AnsiConsole.WriteLine($"CREATING Job '{job.Name}'");

		var sql = $@"
                DECLARE @jobId BINARY(16)
                
                EXEC msdb.dbo.sp_add_job @job_name='{job.Name}'
                    , @enabled=1
                    , @description=N'{job.Description.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n")}' 
                    , @category_name='{job.Category}'
                    , @owner_login_name='{job.Owner}'
                    , @job_id = @jobId OUTPUT

                EXEC msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = '(local)'

                SELECT j.job_id 
                FROM msdb.dbo.sysjobs j
                WHERE j.name = '{job.Name}'
            ";

		return c.ExecuteScalar<Guid>(sql);
	}

	private void DeleteJob(SqlConnection c, string name)
	{
		AnsiConsole.WriteLine($"DELETE job '{name}' (if exists)");

		var sql = $@"
                DECLARE @jobId BINARY(16)

                SELECT @jobId = j.job_id
                FROM msdb.dbo.sysjobs j
                WHERE j.name = '{name}'

                IF NOT (ISNULL(@jobId, 0) = 0)
	                EXEC msdb.dbo.sp_delete_job @jobId, @delete_unused_schedule=1
            ";

		c.Execute(sql);
	}

	private void CreateCategory(SqlConnection c, string category)
	{
		if (string.IsNullOrEmpty(category)) return;

		AnsiConsole.WriteLine($"Creating category '{category}' (if not exists)");

		var sql = $@"
                IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name='{category}' AND category_class=1)
                    EXEC msdb.dbo.sp_add_category @class='JOB', @type='LOCAL', @name='{category}'
            ";
		c.Execute(sql);
	}

	private IEnumerable<Job> Get(PathSettings settings)
	{
		var yaml = new DeserializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();

		var searchPattern = string.IsNullOrEmpty(settings.Name) ? "*.yml" : $"{settings.Name}.yml";

		foreach (var file in Directory.GetFiles(settings.Path, searchPattern))
		{
			var job = yaml.Deserialize<Job>(File.ReadAllText(file));
			if (string.IsNullOrEmpty(job.Name))
				job.Name = Path.GetFileNameWithoutExtension(file);

			yield return job;
		}
	}
}