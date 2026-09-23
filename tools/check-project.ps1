param([string]$EditorRoot='D:\Unity\Hub\Editor\6000.3.24f1\Editor')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$workRoot=Join-Path $projectRoot 'work'
New-Item -ItemType Directory -Path $workRoot -Force | Out-Null
$mono=Join-Path $EditorRoot 'Data\MonoBleedingEdge\bin\mono.exe'
$compiler=Join-Path $EditorRoot 'Data\MonoBleedingEdge\lib\mono\4.5\csc.exe'
$references=Get-ChildItem -LiteralPath (Join-Path $EditorRoot 'Data\Managed\UnityEngine') -Filter '*.dll' | Where-Object { $_.Name -ne 'UnityEditor.dll' -and $_.Name -ne 'UnityEngine.dll' } | ForEach-Object { '-r:"'+$_.FullName+'"' }
$references+= '-r:"'+(Join-Path $EditorRoot 'Data\MonoBleedingEdge\lib\mono\4.5\Facades\netstandard.dll')+'"'
$references+= '-r:"'+(Join-Path $EditorRoot 'Data\Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.2d-cross-platform-2d-6.1.6\ScriptAssemblies\Unity.InputSystem.dll')+'"'
$sources=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'JumpNotIncluded\Assets\Scripts'),(Join-Path $projectRoot 'JumpNotIncluded\Assets\Editor') -Filter '*.cs' | ForEach-Object { '"'+$_.FullName+'"' }
$argsFile=Join-Path $workRoot 'unity-compile.rsp'
@('/nologo','/target:library','/langversion:latest','/define:UNITY_EDITOR,ENABLE_INPUT_SYSTEM',('/out:"'+(Join-Path $workRoot 'UnityCheck.dll')+'"'))+$references+$sources | Set-Content -LiteralPath $argsFile -Encoding utf8
& $mono $compiler ('@'+$argsFile)
if($LASTEXITCODE -ne 0){throw 'C# compilation failed.'}
$entry=Join-Path $workRoot 'RunRules.cs'
'class Program { static void Main() { JumpNotIncluded.EditorTools.RuleChecks.Run(); System.Console.WriteLine("PASS: shuffled ad rotation, cash rewards, $99 cap, coin packs and refunds, six upgrade prices and access, refund/repurchase, independent VIP fire, economy routes, reward and score deduplication, reset."); } }' | Set-Content -LiteralPath $entry -Encoding utf8
$exe=Join-Path $workRoot 'RuleChecks.exe'
& $mono $compiler /nologo /target:exe ('/out:'+$exe) (Join-Path $projectRoot 'JumpNotIncluded\Assets\Scripts\RunModel.cs') (Join-Path $projectRoot 'JumpNotIncluded\Assets\Editor\RuleChecks.cs') $entry
if($LASTEXITCODE -ne 0){throw 'Rule test compilation failed.'}
& $mono $exe
if($LASTEXITCODE -ne 0){throw 'Rule tests failed.'}

