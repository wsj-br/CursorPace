# Test if the app launches without immediate crash
$process = Start-Process -FilePath "dotnet" -ArgumentList "run --project CursorQuotaProgress" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 3

if ($process.HasExited) {
    Write-Host "App crashed with exit code: $($process.ExitCode)"
    exit 1
} else {
    Write-Host "App appears to be running (process still alive after 3 seconds)"
    Stop-Process -Id $process.Id -Force
    exit 0
}
