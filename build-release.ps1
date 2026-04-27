param(
	[string]$ProjectRelativePath = "Explorer++\Explorer++.vcxproj",
	[string]$Configuration = "Release",
	[string]$Platform = "x64",
	[bool]$BuildWinUIHost = $true,
	[bool]$WinUIHostSelfContained = $true,
	[bool]$WinUIHostSingleFile = $false
)

$repo = $PSScriptRoot
$solutionDir = Join-Path $repo "Explorer++"
$projectPath = Join-Path $solutionDir $ProjectRelativePath
$releaseDir = Join-Path $repo "release"
$winUiProjectPath = Join-Path $repo "WinUIHost\ExplorerPlusPlus.WinUIHost.csproj"
$winUiReleaseDir = Join-Path $releaseDir "WinUIHost"
$winUiRuntimeIdentifier = "win-$Platform"
$winUiBuildOutputDir = Join-Path $repo "WinUIHost\bin\$Platform\$Configuration\net8.0-windows10.0.22621.0\$winUiRuntimeIdentifier"

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

if ($LASTEXITCODE -ne 0)
{
	exit $LASTEXITCODE
}

if ($BuildWinUIHost)
{
	if (-not (Test-Path $winUiProjectPath))
	{
		throw "WinUI host project file not found: $winUiProjectPath"
	}

	$null = New-Item -ItemType Directory -Path $winUiReleaseDir -Force

	Push-Location $repo
	try
	{
		Get-ChildItem $winUiReleaseDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

		$publishArguments = @(
			"publish",
			$winUiProjectPath,
			"-c",
			$Configuration,
			"-r",
			$winUiRuntimeIdentifier,
			"-o",
			$winUiReleaseDir,
			"-p:Platform=$Platform",
			"-p:SelfContained=$WinUIHostSelfContained",
			"-p:WindowsAppSDKSelfContained=$WinUIHostSelfContained",
			"-p:PublishSingleFile=$WinUIHostSingleFile",
			"/nologo"
		)

		if ($WinUIHostSingleFile)
		{
			$publishArguments += "-p:IncludeAllContentForSelfExtract=true"
			$publishArguments += "-p:EnableMsixTooling=true"
		}

		dotnet @publishArguments

		if ($LASTEXITCODE -ne 0)
		{
			exit $LASTEXITCODE
		}

		if (-not $WinUIHostSingleFile)
		{
			if (-not (Test-Path $winUiBuildOutputDir))
			{
				throw "WinUI host build output directory not found: $winUiBuildOutputDir"
			}

			Copy-Item (Join-Path $winUiBuildOutputDir "*.xbf") $winUiReleaseDir -Force
			Copy-Item (Join-Path $winUiBuildOutputDir "ExplorerPlusPlus.WinUIHost.pri") $winUiReleaseDir -Force
		}

		Remove-Item (Join-Path $winUiReleaseDir "startup.log") -Force -ErrorAction SilentlyContinue
	}
	finally
	{
		Pop-Location
	}
}

exit 0