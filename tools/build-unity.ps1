param([string]$Unity='D:\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$projectDir=Join-Path $repoRoot 'JumpNotIncluded'
$logDir=Join-Path $repoRoot 'work'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logPath=Join-Path $logDir 'unity-build.log'
$arguments=@('-batchmode','-nographics','-quit','-projectPath',('"'+$projectDir+'"'),'-executeMethod','JumpNotIncluded.EditorTools.ProjectSetup.Build','-logFile',('"'+$logPath+'"'))
$unityProcess=Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
$log=Get-Content -LiteralPath $logPath -Raw
if($log -match 'No valid Unity Editor license'){
    throw 'Unity Personal is not activated. Sign in to Unity Hub and open Settings > Licenses, then retry this build.'
}
if($unityProcess.ExitCode -ne 0 -or $log -notmatch 'JNI Windows build succeeded'){
    Get-Content -LiteralPath $logPath -Tail 60
    throw ('Unity build failed. Exit code: '+$unityProcess.ExitCode)
}
Write-Output (Join-Path $projectDir 'Builds\Windows\JumpNotIncluded.exe')
