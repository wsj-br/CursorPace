$cookie = "WorkosCursorSessionToken=$env:CURSOR_SESSION_TOKEN"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "https://cursor.com/api/usage-summary" `
    -Headers @{
        Cookie = $cookie
        Accept = "application/json"
        "User-Agent" = "Mozilla/5.0"
    }

$response | ConvertTo-Json -Depth 20