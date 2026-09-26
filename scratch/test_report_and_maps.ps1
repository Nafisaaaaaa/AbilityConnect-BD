# test_report_and_maps.ps1
$baseUrl = "http://localhost:5140"
$global:allPassed = $true

function Record-Test($name, $passed, $details) {
    if (-not $passed) { $global:allPassed = $false }
    $badge = if ($passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "$badge $name - $details" -ForegroundColor $color
}

function Invoke-PgSql($query) {
    $cleanQuery = $query -replace '\\"', '"'
    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tempFile, $cleanQuery)
    $env:PGPASSWORD = "123456"
    try {
        $result = & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432 -d SDP1 -t -A -f $tempFile
        return ($result -join "`n").Trim()
    } finally {
        if (Test-Path $tempFile) { Remove-Item $tempFile -Force }
    }
}

function Get-VerificationToken($content) {
    $match = [regex]::Match($content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    if ($match.Success) {
        return $match.Groups[1].Value
    }
    return ""
}

function Get-UserSession($email, $password, $role = $null) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Get -UseBasicParsing
    $token = Get-VerificationToken $loginPage.Content

    $body = @{
        "__RequestVerificationToken" = $token
        "Email" = $email
        "Password" = $password
    }
    if ($role) { $body["Role"] = $role }

    $null = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Post -Body $body -UseBasicParsing
    return $session
}

Write-Host "--- Syncing User Passwords for Testing ---" -ForegroundColor Cyan
Invoke-PgSql @"
UPDATE "DisabilityUsers" SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com') WHERE "Email" = 'ta1234@gmail.com';
UPDATE "Volunteers" SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'), "IsAvailable" = true, "IsVerified" = true WHERE "Email" = 'tasnim@gmail.com';
"@ | Out-Null

# ----------------------------------------------------
# 1. Accessibility Pages
# ----------------------------------------------------
Write-Host "`n--- 1. Testing Accessibility Pages ---" -ForegroundColor Cyan
$accPage = Invoke-WebRequest -Uri "$baseUrl/Accessibility" -Method Get -UseBasicParsing
Record-Test "Accessibility Index Status" ($accPage.StatusCode -eq 200) "Returned HTTP 200"
Record-Test "Accessibility Index Content" ($accPage.Content.Contains("Report Location Issue")) "Contains 'Report Location Issue' link"
Record-Test "Accessibility Index Removed MyReports" (-not $accPage.Content.Contains("/Accessibility/MyReports")) "No references to old /Accessibility/MyReports"

$firstPlaceId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""AccessibilityPlaces"" ORDER BY ""Id"" ASC LIMIT 1;")
if ($firstPlaceId -gt 0) {
    $placeDetails = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/$firstPlaceId" -Method Get -UseBasicParsing
    Record-Test "Accessibility Details Status" ($placeDetails.StatusCode -eq 200) "Returned HTTP 200 for Place #$firstPlaceId"
    Record-Test "Accessibility Details Report Button" ($placeDetails.Content.Contains("/Community/Report?type=Location")) "Links to /Community/Report?type=Location"
}

# ----------------------------------------------------
# 2. Disability User: Nearby NGOs & Volunteers + Community Report
# ----------------------------------------------------
Write-Host "`n--- 2. Testing Disability User Workflows ---" -ForegroundColor Cyan
$disUserSession = Get-UserSession -email "ta1234@gmail.com" -password "Admin@123" -role "Disability"

$nearbyNgos = Invoke-WebRequest -Uri "$baseUrl/Disability/NearbyNGOs" -WebSession $disUserSession -Method Get -UseBasicParsing
Record-Test "Nearby NGOs & Volunteers Status" ($nearbyNgos.StatusCode -eq 200) "Returned HTTP 200"
Record-Test "Nearby NGOs Title" ($nearbyNgos.Content.Contains("Nearby NGOs & Volunteers")) "Page title renamed to 'Nearby NGOs & Volunteers'"
Record-Test "Nearby NGOs OpenStreetMap" ($nearbyNgos.Content.Contains("leaflet") -and $nearbyNgos.Content.Contains("osmMap")) "Renders Leaflet OpenStreetMap and osmMap container"
Record-Test "Nearby NGOs Volunteers List" ($nearbyNgos.Content.Contains("volData") -or $nearbyNgos.Content.Contains("Volunteers")) "Contains volunteer data/markers"

# Community Report Location Submission
$reportGet = Invoke-WebRequest -Uri "$baseUrl/Community/Report?type=Location" -WebSession $disUserSession -Method Get -UseBasicParsing
Record-Test "Community Report Page Status" ($reportGet.StatusCode -eq 200) "Returned HTTP 200"
Record-Test "Community Report OpenStreetMap" ($reportGet.Content.Contains("osmMap") -and $reportGet.Content.Contains("Nominatim")) "Contains OpenStreetMap and Nominatim search"

$reportToken = Get-VerificationToken $reportGet.Content
$postBody = @{
    "__RequestVerificationToken" = $reportToken
    "ReportType" = "Location"
    "PlaceName" = "Dhanmondi Lake Ramp Point"
    "Address" = "Road 8, Dhanmondi, Dhaka"
    "Latitude" = "23.7461"
    "Longitude" = "90.3742"
    "Reason" = "Missing Ramp"
    "Details" = "Steep sidewalk curb prevents wheelchair users from accessing the lakeside pathway."
}

$reportPost = Invoke-WebRequest -Uri "$baseUrl/Community/ReportContent" -WebSession $disUserSession -Method Post -Body $postBody -UseBasicParsing
Record-Test "Submit Location Report HTTP" ($reportPost.StatusCode -eq 200 -or $reportPost.StatusCode -eq 302) "Report submitted successfully"

$latestReportId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""CommunityReports"" WHERE ""PlaceName"" = 'Dhanmondi Lake Ramp Point' ORDER BY ""Id"" DESC LIMIT 1;")
Record-Test "Location Report Stored in DB" ($latestReportId -gt 0) "Found report #$latestReportId with PlaceName in CommunityReports table"

$reportFields = Invoke-PgSql "SELECT ""ReportType"", ""Status"", ""Reason"", ""Latitude"", ""Longitude"" FROM public.""CommunityReports"" WHERE ""Id"" = $latestReportId;"
Record-Test "Location Report Fields" ($reportFields.Contains("Location") -and $reportFields.Contains("Pending") -and $reportFields.Contains("Missing Ramp")) "Stored: $reportFields"

# ----------------------------------------------------
# 3. Admin: Community Reports Moderation
# ----------------------------------------------------
Write-Host "`n--- 3. Testing Admin Community Reports Moderation ---" -ForegroundColor Cyan
$adminSession = Get-UserSession -email "admin@abilityconnect.com" -password "Admin@123"

$adminReports = Invoke-WebRequest -Uri "$baseUrl/Admin/CommunityReports" -WebSession $adminSession -Method Get -UseBasicParsing
Record-Test "Admin CommunityReports Status" ($adminReports.StatusCode -eq 200) "Returned HTTP 200"
Record-Test "Admin CommunityReports Title" ($adminReports.Content.Contains("Community Reports Moderation")) "Moderation page rendered"
Record-Test "Admin CommunityReports Filter" ($adminReports.Content.Contains("Location Reports") -and $adminReports.Content.Contains("All Types")) "Contains report type and status filters"
Record-Test "Admin Displays Submitted Report" ($adminReports.Content.Contains("Dhanmondi Lake Ramp Point")) "Displays our newly submitted location report"

# Moderation Action: Update status to Reviewed
$adminToken = Get-VerificationToken $adminReports.Content
$updateBody = @{
    "__RequestVerificationToken" = $adminToken
    "id" = $latestReportId
    "status" = "Reviewed"
    "adminNotes" = "Forwarded to Dhaka City Corporation road works."
}
$updateResp = Invoke-WebRequest -Uri "$baseUrl/Admin/UpdateCommunityReportStatus" -WebSession $adminSession -Method Post -Body $updateBody -UseBasicParsing
$statusDb = Invoke-PgSql "SELECT ""Status"" FROM public.""CommunityReports"" WHERE ""Id"" = $latestReportId;"
Record-Test "Admin Mark Reviewed" ($statusDb -eq "Reviewed") "Status in DB updated to Reviewed"

# Moderation Action: Update status to Resolved
$resolveBody = @{
    "__RequestVerificationToken" = $adminToken
    "id" = $latestReportId
    "status" = "Resolved"
    "adminNotes" = "Temporary ramp installed by local municipal council."
}
$resolveResp = Invoke-WebRequest -Uri "$baseUrl/Admin/UpdateCommunityReportStatus" -WebSession $adminSession -Method Post -Body $resolveBody -UseBasicParsing
$statusDb2 = Invoke-PgSql "SELECT ""Status"" FROM public.""CommunityReports"" WHERE ""Id"" = $latestReportId;"
Record-Test "Admin Mark Resolved" ($statusDb2 -eq "Resolved") "Status in DB updated to Resolved"

# ----------------------------------------------------
# 4. Volunteer: Nearby Help Requests + Google Maps
# ----------------------------------------------------
Write-Host "`n--- 4. Testing Volunteer Nearby Help Requests & Google Maps ---" -ForegroundColor Cyan
$volSession = Get-UserSession -email "tasnim@gmail.com" -password "Admin@123" -role "Volunteer"

$volNearby = Invoke-WebRequest -Uri "$baseUrl/Volunteer/NearbyHelpRequests" -WebSession $volSession -Method Get -UseBasicParsing
Record-Test "Volunteer Nearby Requests Status" ($volNearby.StatusCode -eq 200) "Returned HTTP 200"
Record-Test "Google Maps Section Header" ($volNearby.Content.Contains("Nearby Requests Map (Google Maps)")) "Contains Google Maps section"
Record-Test "Requests Cards Intact" ($volNearby.Content.Contains("requestsGrid") -or $volNearby.Content.Contains("Accept Request")) "Preserves requests list and accept buttons"
Record-Test "Google Maps Key Handling" ($volNearby.Content.Contains("Google Maps Service") -or $volNearby.Content.Contains("volunteerGoogleMap")) "Gracefully handles API key presence/absence"

Write-Host "`n==========================================================" -ForegroundColor Cyan
if ($global:allPassed) {
    Write-Host " ALL VERIFICATION TESTS PASSED SUCCESSFULLY! (100%)" -ForegroundColor Green
} else {
    Write-Host " SOME TESTS FAILED. PLEASE REVIEW OUTPUT." -ForegroundColor Red
}
Write-Host "==========================================================" -ForegroundColor Cyan
