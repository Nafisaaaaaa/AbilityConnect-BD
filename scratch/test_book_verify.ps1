$baseUrl = "http://localhost:5140"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# 1. Login
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Get -UseBasicParsing
$tokenMatch = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
$token = $tokenMatch.Groups[1].Value

$loginBody = @{
    "__RequestVerificationToken" = $token
    "Email" = "swasthy@gmail.com"
    "Password" = "Admin@123"
    "Role" = "Disability"
}
$loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Post -Body $loginBody -UseBasicParsing

# 2. Get Book Page
$bookPage = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Get -UseBasicParsing
$bookTokenMatch = [regex]::Match($bookPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
$bookToken = $bookTokenMatch.Groups[1].Value

# 3. Post Booking (allow redirect)
$bookDate = (Get-Date).AddDays(3).ToString("yyyy-MM-dd")
$bookBody = @{
    "__RequestVerificationToken" = $bookToken
    "ProviderId" = "1"
    "AppointmentDate" = $bookDate
    "TimeSlot" = "04:00 PM - 04:30 PM"
    "ReasonForVisit" = "Post-stroke mobility physical checkup."
    "PatientNotes" = "Wheelchair ramp needed."
}

$bookResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Post -Body $bookBody -UseBasicParsing
Write-Host "POST BookAppointment followed redirect to: $($bookResp.BaseResponse.ResponseUri)"
Write-Host "Page Status: $($bookResp.StatusCode)"
$hasSuccess = $bookResp.Content.Contains("Your appointment request with") -or $bookResp.Content.Contains("MyAppointments") -or $bookResp.Content.Contains("Dr. Mahbubur Rahman")
Write-Host "Contains Appointment Details / Success: $hasSuccess"
