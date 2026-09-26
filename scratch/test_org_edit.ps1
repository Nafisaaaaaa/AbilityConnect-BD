$baseUrl = "http://localhost:5140"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Get login page
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Get -UseBasicParsing
$token = ""
if ($loginPage.Content -match 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"') {
    $token = $matches[1]
}

Write-Host "Got token: $($token.Substring(0, 15))..."

# Login as red@gmail.com
$loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Post -Body @{
    Email = "red@gmail.com"
    Password = "Admin@123"
    "__RequestVerificationToken" = $token
} -UseBasicParsing

Write-Host "Login Status: $($loginResp.StatusCode)"
Write-Host "Cookies:"
$session.Cookies.GetCookies($baseUrl) | ForEach-Object { Write-Host "$($_.Name) = $($_.Value)" }

# Access Edit
try {
    $editResp = Invoke-WebRequest -Uri "$baseUrl/Jobs/Edit/10" -WebSession $session -Method Get -UseBasicParsing
    Write-Host "Edit Status: $($editResp.StatusCode)"
    Write-Host "Edit Length: $($editResp.Content.Length)"
    if ($editResp.Content.Contains("Customer Support")) {
        Write-Host "Found job title in edit form!" -ForegroundColor Green
    } else {
        Write-Host "Did not find job title. First 500 chars:"
        Write-Host $editResp.Content.Substring(0, [Math]::Min(500, $editResp.Content.Length))
    }
} catch {
    Write-Host "Edit Error: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)"
        Write-Host "Location: $($_.Exception.Response.Headers['Location'])"
    }
}
