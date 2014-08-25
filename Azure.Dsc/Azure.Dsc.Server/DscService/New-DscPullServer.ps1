Param([string]$SubjectName = "dscpullserver.cloudapp.net")

Import-Module -Name xPSDesiredStateConfiguration -Force -Verbose

Configuration Sogeti_DscWebService 
{ 
    param ( 
        [string[]]$NodeName = 'localhost', 
 
        [ValidateNotNullOrEmpty()] 
        [string] $certificateThumbPrint 
    ) 
 
    Import-DSCResource -ModuleName xPSDesiredStateConfiguration
 
    Node $NodeName 
    { 
        Service Assert_W3LOGSVC
        {
            Name = "w3logsvc"
            State = "Running"
            StartupType = "Automatic"
            BuiltInAccount = "LocalSystem"
        }

        Service Assert_W3SVC
        {
            Name = "W3SVC"
            State = "Running"
            StartupType = "Automatic"
            BuiltInAccount = "LocalSystem"
            DependsOn = "[Service]Assert_W3LOGSVC"
        }

        WindowsFeature Assert_IIS
        {
            Ensure = "Present"
            Name = "Web-Server"
        }

        WindowsFeature DSCServiceFeature 
        { 
            Ensure = "Present" 
            Name   = "DSC-Service"  
            DependsOn = @("[WindowsFeature]Assert_IIS","[Service]Assert_W3SVC")          
        } 
 
        xDscWebService PSDSCPullServer 
        { 
            Ensure                  = "Present" 
            EndpointName            = "PSDSCPullServer" 
            Port                    = 8080 
            PhysicalPath            = "$env:SystemDrive\inetpub\wwwroot\PSDSCPullServer" 
            CertificateThumbPrint   = $certificateThumbPrint          
            ModulePath              = "$env:PROGRAMFILES\WindowsPowerShell\DscService\Modules" 
            ConfigurationPath       = "$env:PROGRAMFILES\WindowsPowerShell\DscService\Configuration"             
            State                   = "Started" 
            DependsOn               = "[WindowsFeature]DSCServiceFeature"                         
        } 
 
        xDscWebService PSDSCComplianceServer 
        { 
            Ensure                  = "Present" 
            EndpointName            = "PSDSCComplianceServer" 
            Port                    = 9080 
            PhysicalPath            = "$env:SystemDrive\inetpub\wwwroot\PSDSCComplianceServer" 
            CertificateThumbPrint   = "AllowUnencryptedTraffic" 
            State                   = "Started" 
            IsComplianceServer      = $true 
            DependsOn               = @("[WindowsFeature]DSCServiceFeature","[xDSCWebService]PSDSCPullServer") 
        } 
    } 
}

$x509 = Get-ChildItem Cert:\LocalMachine\My | Where Subject -Eq "CN=$SubjectName"

Sogeti_DscWebService -certificateThumbPrint $x509.Thumbprint -OutputPath .

Start-DscConfiguration -wait -Verbose -ComputerName localhost -Path .\Sogeti_DscWebService

$ipAddress = Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp | Select -ExpandProperty IPAddress

Invoke-Expression -Command "netsh http delete urlacl https://$($ipAddress):8080/"
Invoke-Expression -Command "netsh http delete urlacl http://$($ipAddress):9080/"

Copy-Item -Path "$env:ROLEROOT\approot\DscService\Modules\*" -Exclude "*.zip" -Recurse -Destination "$env:ProgramFiles\WindowsPowerShell\DscService\Modules" -Force