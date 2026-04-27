$repo = $PSScriptRoot
$solutionDir = Join-Path $repo "Explorer++"
$projectPath = Join-Path $solutionDir "Explorer++\Explorer++.vcxproj"
$releaseDir = Join-Path $repo "release"

$msbuildCandidates = @(
	"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
	"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
	"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
)

$msbuildPath = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $msbuildPath)
{
	throw "MSBuild.exe was not found in the expected Visual Studio 2022 locations."
}

& $msbuildPath $projectPath /p:Configuration=Release /p:Platform=x64 /p:SolutionDir="$solutionDir\\" /p:OutDir="$releaseDir\\" /t:Build /m /nologo /v:minimal
exit $LASTEXITCODE