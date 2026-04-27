param(
	[string]$ProjectRelativePath = "Explorer++\Explorer++.vcxproj",
	[string]$Configuration = "Release",
	[string]$Platform = "x64"
)

$repo = $PSScriptRoot
$solutionDir = Join-Path $repo "Explorer++"
$projectPath = Join-Path $solutionDir $ProjectRelativePath
$releaseDir = Join-Path $repo "release"

if (-not (Test-Path $projectPath))
{
	throw "Project file not found: $projectPath"
}

$msbuildPath = $null
$vswhereCandidates = @(
	(Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"),
	(Join-Path $env:ProgramFiles "Microsoft Visual Studio\Installer\vswhere.exe")
)

foreach ($vswherePath in $vswhereCandidates)
{
	if (-not $vswherePath -or -not (Test-Path $vswherePath))
	{
		continue
	}

	$msbuildPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\amd64\MSBuild.exe" | Select-Object -First 1

	if ($msbuildPath)
	{
		break
	}
}

if (-not $msbuildPath)
{
	$msbuildCandidates = @(
		"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe",
		"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
	)

	$msbuildPath = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $msbuildPath)
{
	throw "MSBuild.exe was not found in the expected Visual Studio 2022 locations."
}

& $msbuildPath $projectPath /p:Configuration=$Configuration /p:Platform=$Platform /p:SolutionDir="$solutionDir\\" /p:OutDir="$releaseDir\\" /t:Build /m /nologo /v:minimal
exit $LASTEXITCODE