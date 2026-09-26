$baseUrl = "http://localhost:5140"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# 1. Login as Disability User
Write-Host "Logging in as Disability User..."
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Get -UseBasicParsing
$tokenMatch = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
$token = $tokenMatch.Groups[1].Value

$loginBody = @{
    "__RequestVerificationToken" = $token
    "Email" = "swasthy@gmail.com"
    "Password" = "Admin@123"
    "Role" = "Disability"
}

$loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Post -Body $loginBody -MaximumRedirection 0 -UseBasicParsing -ErrorAction SilentlyContinue
Write-Host "Login response status: $($loginResp.StatusCode)"

# 2. Get BookAppointment page for Provider 1
Write-Host "Fetching BookAppointment page for Provider 1..."
$bookPage = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Get -UseBasicParsing
Write-Host "Book page status: $($bookPage.StatusCode)"
$bookTokenMatch = [regex]::Match($bookPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
$bookToken = $bookTokenMatch.Groups[1].Value

# 3. Submit appointment booking
Write-Host "Submitting appointment booking to /Healthcare/BookAppointment/1 ..."
$bookBody = @{
    "__RequestVerificationToken" = $bookToken
    "ProviderId" = "1"
    "AppointmentDate" = (Get-Date).AddDays(2).ToString("yyyy-MM-dd")
    "TimeSlot" = "04:00 PM - 04:30 PM"
    "ReasonForVisit" = "Consultation for physical mobility rehabilitation."
    "PatientNotes" = "Wheelchair ramp needed."
}

try {
    $bookResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Post -Body $bookBody -MaximumRedirection 0 -UseBasicParsing -ErrorAction Stop
    Write-Host "Success Status: $($bookResp.StatusCode)"
    Write-Host "Location: $($bookResp.Headers.Location)"
} catch {
    Write-Host "Error occurred during POST:" -ForegroundColor Red
    $errResp = $_.Exception.Response
    if ($errResp) {
        $stream = $errResp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        Write-Host "Status code: $($errResp.StatusCode)"
        [System.IO.File]::WriteAllText("scratch/error_dump.html", $body)
        Write-Host "Full error dumped to scratch/error_dump.html"
    } else {
        Write-Host $_.Exception.Message
    }
}
