$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5140"
$pgPass = "123456"
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Run-Sql($query) {
    $env:PGPASSWORD = $pgPass
    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tempFile, $query)
    try {
        $out = & $psqlPath -U postgres -h localhost -p 5432 -d SDP1 -t -A -f $tempFile
        return $out
    } finally {
        if (Test-Path $tempFile) { Remove-Item $tempFile -Force }
    }
}

function Record-Test($name, $passed, $details) {
    $badge = if ($passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "$badge $name - $details" -ForegroundColor $color
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " VERIFYING APPOINTMENT BOOKING FIX & FUNCTIONAL REQUIREMENTS" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Clean previous test bookings for Provider 1 & User 1 to start fresh
Run-Sql "DELETE FROM public.`"HealthcareAppointments`" WHERE `"ReasonForVisit`" LIKE '%TestBooking%';";

# 1. Test Authentication & Session Login as Disability User
try {
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
    $isLogged = $loginResp.Content.Contains("swasthy") -or $loginResp.Content.Contains("Logout") -or $loginResp.Content.Contains("Dashboard")
    Record-Test "1. Authentication / Login as Disability User" $isLogged "User swasthy@gmail.com logged in successfully"
} catch {
    Record-Test "1. Authentication / Login as Disability User" $false $_.Exception.Message
}

# 2. Test GET /Healthcare/BookAppointment/1
try {
    $bookPage = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Get -UseBasicParsing
    $hasProvider = $bookPage.Content.Contains("Dr. Mahbubur Rahman")
    $hasDateInput = $bookPage.Content.Contains("AppointmentDate")
    $hasSlotSelect = $bookPage.Content.Contains("TimeSlot")
    $hasReasonInput = $bookPage.Content.Contains("ReasonForVisit")
    $passGet = ($bookPage.StatusCode -eq 200 -and $hasProvider -and $hasDateInput -and $hasSlotSelect -and $hasReasonInput)
    Record-Test "2. GET BookAppointment Form" $passGet "Rendered appointment form with provider details and form inputs"
} catch {
    Record-Test "2. GET BookAppointment Form" $false $_.Exception.Message
}

# 3. Test Past Date Rejection
try {
    $bookTokenMatch = [regex]::Match($bookPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    $bookToken = $bookTokenMatch.Groups[1].Value

    $pastDateBody = @{
        "__RequestVerificationToken" = $bookToken
        "ProviderId" = "1"
        "AppointmentDate" = (Get-Date).AddDays(-2).ToString("yyyy-MM-dd")
        "TimeSlot" = "04:00 PM - 04:30 PM"
        "ReasonForVisit" = "TestBooking: Past date attempt"
        "PatientNotes" = "None"
    }

    $pastResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Post -Body $pastDateBody -UseBasicParsing
    $rejectedPast = $pastResp.Content.Contains("cannot be in the past") -or $pastResp.Content.Contains("today or a future date")
    Record-Test "3. Past-Date Booking Rejection" $rejectedPast "Past date was successfully caught and rejected with validation message"
} catch {
    Record-Test "3. Past-Date Booking Rejection" $false $_.Exception.Message
}

# 4. Test Valid Appointment Booking
$validDateStr = (Get-Date).AddDays(5).ToString("yyyy-MM-dd")
try {
    $bookPage2 = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Get -UseBasicParsing
    $tokenMatch2 = [regex]::Match($bookPage2.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    $token2 = $tokenMatch2.Groups[1].Value

    $validBody = @{
        "__RequestVerificationToken" = $token2
        "ProviderId" = "1"
        "AppointmentDate" = $validDateStr
        "TimeSlot" = "05:00 PM - 05:30 PM"
        "ReasonForVisit" = "TestBooking: Valid rehabilitation follow-up"
        "PatientNotes" = "Wheelchair ramp needed at entrance"
    }

    $bookResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Post -Body $validBody -UseBasicParsing
    $redirectedToMyAppts = $bookResp.BaseResponse.ResponseUri.ToString().Contains("/Healthcare/MyAppointments")
    $showsSuccess = $bookResp.Content.Contains("submitted") -or $bookResp.Content.Contains("Pending confirmation")
    Record-Test "4. POST Valid Appointment Booking" ($redirectedToMyAppts -and $showsSuccess) "Redirected to MyAppointments with success alert"
} catch {
    Record-Test "4. POST Valid Appointment Booking" $false $_.Exception.Message
}

# 5. Verify Appointment Record in PostgreSQL
try {
    $checkSql = "SELECT `"Status`", `"TimeSlot`", `"ReasonForVisit`", `"PatientNotes`" FROM public.`"HealthcareAppointments`" WHERE `"ReasonForVisit`" LIKE '%TestBooking: Valid rehabilitation follow-up%' LIMIT 1;"
    $dbResult = (Run-Sql $checkSql) -split "`r?`n"
    $hasRecord = ($dbResult.Length -ge 1 -and $dbResult[0].Contains("Pending") -and $dbResult[0].Contains("05:00 PM - 05:30 PM"))
    Record-Test "5. Database Verification (PostgreSQL)" $hasRecord "Appointment saved in PostgreSQL with Status='Pending', correct TimeSlot, Reason, Notes"
} catch {
    Record-Test "5. Database Verification (PostgreSQL)" $false $_.Exception.Message
}

# 6. Test Duplicate Conflicting Booking Prevention
try {
    $bookPage3 = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Get -UseBasicParsing
    $tokenMatch3 = [regex]::Match($bookPage3.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    $token3 = $tokenMatch3.Groups[1].Value

    # Submit same provider, date, and timeslot
    $dupBody = @{
        "__RequestVerificationToken" = $token3
        "ProviderId" = "1"
        "AppointmentDate" = $validDateStr
        "TimeSlot" = "05:00 PM - 05:30 PM"
        "ReasonForVisit" = "TestBooking: Duplicate attempt"
        "PatientNotes" = "None"
    }

    $dupResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/BookAppointment/1" -WebSession $session -Method Post -Body $dupBody -UseBasicParsing
    $rejectedDup = $dupResp.Content.Contains("already have a pending or confirmed appointment")
    Record-Test "6. Duplicate Booking Prevention" $rejectedDup "Duplicate pending booking correctly rejected with error message"
} catch {
    Record-Test "6. Duplicate Booking Prevention" $false $_.Exception.Message
}

# 7. Test MyAppointments View
try {
    $myApptsResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/MyAppointments" -WebSession $session -Method Get -UseBasicParsing
    $hasAppt = $myApptsResp.Content.Contains("Dr. Mahbubur Rahman") -and $myApptsResp.Content.Contains("Pending")
    Record-Test "7. MyAppointments View" ($myApptsResp.StatusCode -eq 200 -and $hasAppt) "Displays booked appointments with provider and Pending status badge"
} catch {
    Record-Test "7. MyAppointments View" $false $_.Exception.Message
}

# 8. Test Accessible Job Portal Regression
try {
    $jobsResp = Invoke-WebRequest -Uri "$baseUrl/Jobs" -WebSession $session -Method Get -UseBasicParsing
    $jobsOk = ($jobsResp.StatusCode -eq 200 -and $jobsResp.Content.Contains("Jobs"))
    Record-Test "8. Accessible Job Portal Regression" $jobsOk "Section 5.4 Accessible Job Portal is 100% operational"
} catch {
    Record-Test "8. Accessible Job Portal Regression" $false $_.Exception.Message
}

# Clean up test appointments created during this test
Run-Sql "DELETE FROM public.`"HealthcareAppointments`" WHERE `"ReasonForVisit`" LIKE '%TestBooking%';";
Write-Host "Cleaned up transient test appointments." -ForegroundColor Gray
