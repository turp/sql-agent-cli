# SQL Agent CLI

Console application used to list, extract and import SQL Agent jobs.

## List

```
sql-agent-cli list -s server.intel.com,1433
```

## Export

```
sql-agent-cli export --server server.intel.com,1433 --path .\jobs
```

## Import

```
sql-agent-cli import --server server.intel.com,1433 --path .\jobs
```

## Sql Agent Job YAML file

```yaml
# if omitted, will default to filename
name: FooBar2
# use the |- to enable multiline
description: |-
  Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris id pharetra sem. Interdum et malesuada fames ac ante ipsum primis in faucibus. Morbi ante tortor, imperdiet eget laoreet ac, vehicula in metus. Morbi nec tellus nulla. 
  Nulla placerat vitae lectus vitae fringilla. Vivamus scelerisque enim ut nisi mattis rutrum. 

  Sed imperdiet orci enim, et condimentum sem pharetra ac. Maecenas et eros eu risus malesuada molestie. Proin ex mauris, vehicula sed ipsum ut, pharetra placerat dui. Suspendisse quis fermentum nisl. 

  Sed molestie lacus posuere bibendum condimentum.
# if category does not exist, it will be created
category: whatever
# login must already exist on target database server
owner: sa
# job steps
steps:
  # name of step
  - name: SPEED Events
    # CmdExec, TSQL or SSIS
    subsystem: CmdExec
    command: CALL %MitExtractExe% Server=SomeServer name=SpeedEventsXrb site=SITE1
  - name: Truncate Tables
    subsystem: TSQL
    command: |-
      truncate table dbo.Table1
      truncate table dbo.Table2
      --truncate table dbo.Table3
    # database name is optional
    database: MyDb
    # proxy must already be defined on the target server
    proxy: my_proxy
  - name: Load Proc
    subsystem: TSQL
    command: |-
      exec MyDb.dbo.spAddLoaderRun 'foobar'
      exec MyDb.dbo.spPushEvent
schedules:
  - name: Daily
    interval: daily
  - name: Daily at 2:15pm
    interval: at 14:15
    target: dev
    enabled: false
  - name: Hourly at 14 past
    interval: every 1 hours at 12:14
    target: prod
    enabled: true
```

Job Schedules

Schedules are defined in human readable format. Below are some examples:

| schedule                           | description                                                  |
| ---------------------------------- | ------------------------------------------------------------ |
| daily                              | everyday at midnight                                         |
| daily at 00:00                     | everyday at midnight                                         |
| at 14:15                           | everyday at 2:15pm                                           |
| every day at 14:15                 | everyday at 2:15pm                                           |
| every 4 days at 14:15              | every 4 days at 2:15pm                                       |
| every 3 hours                      | repeats every 3 hours starting at midnight                   |
| every 3 hours starting at 01:05    | repeats every 3 hours starting at 1:05am                     |
| every 5 minutes from 2:05 to 19:09 | repeats every 5 minutes starting at 2:05am and 7:09pm        |
| every 10 seconds                   | daily, every 10 seconds                                      |
| every week at 09:15                | every sunday at 9:15am                                       |
| every 3 weeks at 14:15             | every 3rd sunday at 2:15pm                                   |
| every week on mon,wed,fri at 14:15 | every monday, Wednesday and Friday at 2:15pm                 |
| every month at 09:15               | on the first day of every month at 9:15am                    |
| every 3 months on 4 at 14:15       | exectued every 3 months, on the first day of month at 2:15pm |
