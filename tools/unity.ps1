# Runs Unity in batch mode against this project and prints the PulseDesk / error lines.
#   tools\unity.ps1 -Method PulseDesk.EditorTools.PulseDeskAutomation.Stage1
#   tools\unity.ps1 -Tests
param([string]$Method, [switch]$Tests, [string]$Name = "run")

$project = Split-Path $PSScriptRoot -Parent
$version = (Get-Content "$project\ProjectSettings\ProjectVersion.txt" | Select-Object -First 1) -replace 'm_EditorVersion:\s*', ''
$unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
$log = "$project\Logs\batch-$Name.log"
$a = @('-batchmode', '-projectPath', $project, '-logFile', $log, '-buildTarget', 'Android')
if ($Tests) { $a += @('-runTests', '-testPlatform', 'EditMode', '-testResults', "$project\Logs\test-results.xml") }
else { $a += @('-executeMethod', $Method) }

# Not -Wait: that also waits for helpers Unity leaves behind (adb, compiler server).
$p = Start-Process -FilePath $unity -ArgumentList $a -PassThru
$p.WaitForExit()
"exit code: $($p.ExitCode)"
Select-String -Path $log -Pattern 'error CS\d+|^PulseDesk[: ]|Scripts have compiler errors|Build completed|BuildFailedException|Exception:' |
    ForEach-Object { $_.Line } | Select-Object -Unique | Select-Object -First 60
