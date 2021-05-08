using namespace Paket
Add-Type -path /home/vscode/.nuget/packages/paket/6*/tools/*/any/paket.dll

$dependencies=[Dependencies]::Locate()
$dependencies.GetDirectDependencies()
$dependencies.UpdateGroup('Main')
