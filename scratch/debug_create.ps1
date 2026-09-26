$baseUrl = "http://localhost:5140"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Login as swasthy
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Get -UseBasicParsing
$token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$loginBody = @{
    "__RequestVerificationToken" = $token
    "Email" = "swasthy@gmail.com"
    "Password" = "Admin@123"
    "Role" = "Disability"
}
$loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Post -Body $loginBody -UseBasicParsing
Write-Host "Logged in swasthy. Redirected to: $($loginResp.BaseResponse.ResponseUri)"

# Get create page
$createPage = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Create" -WebSession $session -Method Get -UseBasicParsing
$createToken = [regex]::Match($createPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value
Write-Host "Create Token: $createToken"

# Post create
$dateStr = (Get-Date).AddDays(2).ToString("yyyy-MM-dd")
$reqData = @{
    "__RequestVerificationToken" = $createToken
    "AssistanceType" = "Hospital Visits"
    "Title" = "Assistance attending physiotherapy at CRP Savar"
    "Description" = "Need an experienced volunteer to assist with wheelchair transfer and outpatient counter navigation."
    "RequestDate" = $dateStr
    "RequestTime" = "10:30 AM"
    "Location" = "CRP Savar Main OPD"
    "City" = "Dhaka"
    "District" = "Dhaka"
    "Urgency" = "High"
}

try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Create" -WebSession $session -Method Post -Body $reqData -UseBasicParsing
    Write-Host "Create POST succeeded! Status: $($resp.StatusCode), URL: $($resp.BaseResponse.ResponseUri)"
} catch {
    Write-Host "Create POST FAILED!" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception)" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}
