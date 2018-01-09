[environment]::SetEnvironmentVariable("StatsDServer","docker01","User")
[environment]::SetEnvironmentVariable("StatsDPort","8125","User")
[environment]::SetEnvironmentVariable("StatsDPrefix","akka-demo","User")
[environment]::SetEnvironmentVariable("BUSINESS_RULES_FILENAME","c:\temp\business_rules_map.rules","User")
[environment]::SetEnvironmentVariable("COMMANDS_TO_RULES_FILENAME","c:\temp\commands_to_rules.rules","User")

Copy-Item -Path .\Loaner\BoundedContexts\MaintenanceBilling\BusinessRules\BusinessRulesMap.txt C:\Temp\business_rules_map.rules -Force
Copy-Item -Path .\Loaner\BoundedContexts\MaintenanceBilling\BusinessRules\CommandToBusinessRuleMap.txt C:\Temp\commands_to_rules.rules -Force
##Check it's set
[environment]::GetEnvironmentVariable("StatsDServer","User")
[environment]::GetEnvironmentVariable("StatsDPort","User")
[environment]::GetEnvironmentVariable("StatsDPrefix","User")
[environment]::GetEnvironmentVariable("BUSINESS_RULES_FILENAME","User")
[environment]::GetEnvironmentVariable("COMMANDS_TO_RULES_FILENAME","User")
