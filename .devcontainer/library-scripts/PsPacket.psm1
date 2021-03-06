function Add-PaketReferences {
    param (
        [parameter(Position = 0)]
        [ArgumentCompleter( {
                [OutputType([System.Management.Automation.CompletionResult])]  # zero to many
                param(
                    [string] $CommandName,
                    [string] $ParameterName,
                    [string] $WordToComplete,
                    [System.Management.Automation.Language.CommandAst] $CommandAst,
                    [System.Collections.IDictionary] $FakeBoundParameters
                )
                $responce = Invoke-RestMethod azuresearch-usnc.nuget.org/autocomplete -Method Get -Body @{
                    q           = $WordToComplete
                    prerelease  = $true
                    semVerLevel = 2.0.0
                }
                foreach ($result in $responce.data) {
                    [System.Management.Automation.CompletionResult]$result
                }
            })]
        $id,
        [parameter(Position = 1)]
        [ArgumentCompleter( {
                [OutputType([System.Management.Automation.CompletionResult])]  # zero to many
                param(
                    [string] $CommandName,
                    [string] $ParameterName,
                    [string] $WordToComplete,
                    [System.Management.Automation.Language.CommandAst] $CommandAst,
                    [System.Collections.IDictionary] $FakeBoundParameters
                )

                (Get-ChildItem -path $psEditor.Workspace.Path -Include *.*proj -Recurse -file).where{
                    $_.BaseName -match ''
                }.foreach{
                    $relativePath = [System.IO.Path]::GetRelativePath($psEditor.Workspace.Path, $_.fullname)
                    [System.Management.Automation.CompletionResult]::new(
                        $relativePath,
                        $_.Name,
                        [System.Management.Automation.CompletionResultType]::ParameterValue,
                        $relativePath)
                }
            })]
        $project
    )
    Set-Location $psEditor.Workspace.Path
    dotnet paket add $id -p $project
    Set-Location -
}