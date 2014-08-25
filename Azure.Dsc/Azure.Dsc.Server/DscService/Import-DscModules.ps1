$log = "$PSScriptRoot\ps-log.txt"

if(-not (Test-Path Env:\ROLEROOT))
{
    [System.Environment]::SetEnvironmentVariable("ROLEROOT", "E:")
	Add-Content -Path $log -Value "ROLEROOT: $env:ROLEROOT." -Force
}

if(-not (Test-Path $env:ROLEROOT\approot\DscService\Modules))
{
    mkdir $env:ROLEROOT\approot\DscService\Modules

	Add-Content -Path $log -Value "Created directory $env:ROLEROOT\approot\DscService\Modules." -Force
}

Add-Content -Path $log -Value "Downloading DSC Resource Kit..." -Force

(New-Object Net.WebClient).DownloadFile("https://sogetiiac.blob.core.windows.net/public/DSC_Resource_Kit_Wave_4.zip","$env:ROLEROOT\approot\DscService\Modules\DSC_Resource_Kit_Wave_4.zip")

if(Test-Path "$env:ROLEROOT\approot\DscService\Modules\DSC_Resource_Kit_Wave_4.zip")
{
	Add-Content -Path $log -Value "DSC Resource Kit downloaded and stored as $env:ROLEROOT\approot\DscService\Modules\DSC_Resource_Kit_Wave_4.zip." -Force
}
else
{
	Add-Content -Path $log -Value "[ERROR] Something went wrong while downloading the DSC Resource Kit." -Force
}
 
Add-Content -Path $log -Value  "Extracting DSC Resource Kit..." -Force

(new-object -com shell.application).namespace("$env:ROLEROOT\approot\DscService\").CopyHere((new-object -com shell.application).namespace("$env:ROLEROOT\approot\DscService\Modules\DSC_Resource_Kit_Wave_4.zip").Items(),16)

if(Test-Path "$env:ROLEROOT\approot\DscService\Modules\xPSDesiredStateConfiguration\")
{
	Add-Content -Path $log -Value "DSC Resource Kit extraced in  $env:ROLEROOT\approot\DscService\Modules\." -Force
}
else
{
	Add-Content -Path $log -Value "[ERROR] Something went wrong while extracting the DSC Resource Kit." -Force
}

Add-Content -Path $log -Value "Copying DSC Resource Kit..." -Force

Copy-Item -Path "$env:ROLEROOT\approot\DscService\Modules\*" -Exclude "*.zip" -Recurse -Destination "$env:ProgramFiles\WindowsPowerShell\Modules" -Force

if(Test-Path "$env:ProgramFiles\WindowsPowerShell\Modules\xPSDesiredStateConfiguration\")
{
	Add-Content -Path $log -Value "DSC Resource Kit copied to $env:ProgramFiles\WindowsPowerShell\Modules\." -Force
}
else
{
	Add-Content -Path $log -Value "[ERROR] Something went wrong while copying the DSC Resource Kit." -Force
}