[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = (property Configuration Release)
)

$script:hasNotSetup = -not (Test-Path $PWD/*.sln)

# Synopsis: Setup repository.
task Setup -If ($script:hasNotSetup) {
    Write-Build Green 'Setup start...'

    $tasks = @(
        @{
            Name = 'Install Paket'
            Jobs = {
                dotnet new tool-manifest --force
                dotnet tool install --local Paket --version 6.0.0-rc001
                dotnet tool restore
            }
        }
        @{
            Name = 'Setup Packet'
            Jobs = {
                dotnet paket init
                dotnet paket install
                dotnet restore
            }
        }
        @{
            Name = 'Add Expecto Templates'
            Jobs = {
                dotnet new -i Expecto.Template::*
            }
        }

        @{
            Name = 'Create solution'
            Jobs = {
                dotnet new sln
            }
        }
        @{
            Name = 'Create first Project'
            Jobs = {
                $SolutionName = $BuildRoot | Split-Path -Leaf

                Remove-Item src -Force -Recurse
                Remove-Item tests -Force -Recurse

                dotnet new classlib -lang=f# -o "src/$SolutionName"
                dotnet new expecto -o "tests/$SolutionName.Tests"
                $project = "src/$SolutionName/$SolutionName.fsproj"
                $testProject = "tests/$SolutionName.Tests/$SolutionName.Tests.fsproj"

                # fix target flamework
                [xml]$expectoProj = Get-Content $testProject
                $expectoProj.Project.PropertyGroup.TargetFramework = 'net5.0'
                $expectoProj.Save($testProject)

                dotnet add $testProject reference $project
                dotnet sln add $project
                dotnet sln add $testProject                
            }
        }
    )

    Invoke-Build * {
        foreach ($taskInfo in $tasks) {
            task @taskInfo
        }
    }
}

# Synopsis: Build the project.
task Build {
    exec { dotnet build -c $Configuration }
}

# Synopsis: Remove temp files.
task Clean {
    remove ./*/*/bin, ./*/*/obj
}

# Synopsis: Run tests.
task Test Build, {
    exec { dotnet test }
}

# Synopsis: Build and clean.
task . Build, Clean