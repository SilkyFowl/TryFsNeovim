Install-Module PSReadLine -AllowPrerelease -AcceptLicense -AllowClobber -Force
$initProjectName=$PWD | Split-Path -Leaf

#Init Profile
New-Item $profile.CurrentUserAllHosts -Force
Copy-Item .devcontainer/library-scripts/profile.ps1 $profile.CurrentUserAllHosts -Force

# Install InvokeBuild
Install-Module InvokeBuild -Force
Install-Script New-VSCodeTask -Force
Install-Script -Name Invoke-TaskFromVSCode -Force
Install-Script -Name Debug-Error -Force
Install-Script Invoke-Build.ArgumentCompleters -Force


# Init Build Script
Copy-Item ./.devcontainer/library-scripts/BuildTemplate.ps1 "$initProjectName.build.ps1" -Force
~/.local/share/powershell/Scripts/New-VSCodeTask.ps1
Invoke-Build Setup