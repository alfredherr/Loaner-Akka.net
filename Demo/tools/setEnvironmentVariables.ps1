[environment]::SetEnvironmentVariable("StatsDServer","docker01","User")
[environment]::SetEnvironmentVariable("StatsDPort","8125","User")
[environment]::SetEnvironmentVariable("StatsDPrefix","akka-demo","User")

##Check it's set
[environment]::GetEnvironmentVariable("StatsDServer","User")
[environment]::GetEnvironmentVariable("StatsDPort","User")
[environment]::GetEnvironmentVariable("StatsDPrefix","User")
