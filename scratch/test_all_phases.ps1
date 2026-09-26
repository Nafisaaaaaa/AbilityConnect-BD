# ========================================================
# Comprehensive Verification Script for SDP1:
# Part 1: Job Portal Admin Permission Correction
# Part 2: Section 5.6 Accessibility Map
# Part 3: Section 5.7 Volunteer Matching System
# Regression Testing: Healthcare, Jobs, Dashboards
# ========================================================

$baseUrl = "http://localhost:5140"
$testResults = [System.Collections.Generic.List[PSCustomObject]]::new()

function Record-Test($category, $name, $passed, $details) {
    $result = [PSCustomObject]@{
        Category = $category
        TestName = $name
        Status   = if ($passed) { "PASS" } else { "FAIL" }
        Details  = $details
    }
    $testResults.Add($result)
    $badge = if ($passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "$badge [$category] $name - $details" -ForegroundColor $color
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

# 1. Sync credentials in DB
Write-Host "Syncing test credentials and roles in database..." -ForegroundColor Gray
Invoke-PgSql @"
UPDATE "DisabilityUsers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com');

UPDATE "Volunteers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsAvailable" = true,
    "IsVerified" = true,
    "City" = 'Dhaka',
    "District" = 'Dhaka',
    "AssistanceTypes" = 'Hospital Visits, Shopping Assistance, Travel Assistance, Document Submission, Daily Support'
WHERE "Email" = 'halima@gmail.com';

UPDATE "Volunteers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsAvailable" = true,
    "IsVerified" = true,
    "City" = 'Chittagong',
    "District" = 'Chittagong',
    "AssistanceTypes" = 'Hospital Visits, Shopping Assistance, Travel Assistance, Document Submission, Daily Support'
WHERE "Email" = 'nafu@gmail.com';

UPDATE "Organizations"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsVerified" = true
WHERE "Email" = 'red@gmail.com';
"@ | Out-Null

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING E2E VERIFICATION: PARTS 1, 2, AND 3" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# ========================================================
# PART 1: JOB PORTAL ADMIN PERMISSION CORRECTION
# ========================================================
Write-Host "`n--- Testing Part 1: Job Portal Admin Permission Correction ---" -ForegroundColor Yellow

$adminSession = Get-UserSession "admin@abilityconnect.com" "Admin@123"

# Test 1.1: Admin Jobs & Programs Page (Edit button must NOT exist)
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Admin/JobsAndPrograms" -WebSession $adminSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasEditButton = $content -match 'href="/Jobs/Edit/'
    $hasViewButton = $content -match 'href="/Jobs/Details/'
    $hasDeleteButton = $content -match 'href="/Jobs/Delete/'
    
    $pass = ($resp.StatusCode -eq 200 -and -not $hasEditButton -and $hasViewButton -and $hasDeleteButton)
    Record-Test "Part 1" "Admin UI: Edit Job button removed" $pass "Edit Job link removed from table; View and Delete preserved"
} catch {
    Record-Test "Part 1" "Admin UI: Edit Job button removed" $false $_.Exception.Message
}

# Test 1.2: Admin Direct URL Edit GET must be blocked
try {
    $jobId = 10
    $resp = Invoke-WebRequest -Uri "$baseUrl/Jobs/Edit/$jobId" -WebSession $adminSession -Method Get -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $redirectUrl = $resp.Headers["Location"]
    $pass = ($resp.StatusCode -eq 302 -and $redirectUrl -match "Admin")
    Record-Test "Part 1" "Admin direct URL GET /Jobs/Edit blocked" $pass "Status 302 redirected to $redirectUrl"
} catch {
    if ($_.Exception.Response.StatusCode -eq 302) {
        $loc = $_.Exception.Response.Headers["Location"]
        Record-Test "Part 1" "Admin direct URL GET /Jobs/Edit blocked" $true "Status 302 redirected to $loc"
    } else {
        Record-Test "Part 1" "Admin direct URL GET /Jobs/Edit blocked" $false $_.Exception.Message
    }
}

# Test 1.3: Admin Details view (Edit Posting button must NOT exist, Review Applicants and Delete present)
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Jobs/Details/10" -WebSession $adminSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasEditPosting = $content.Contains("Edit Posting")
    $hasReviewApplicants = $content.Contains("Review Applicants")
    $hasDeleteJob = $content.Contains("Delete Job")
    
    $pass = ($resp.StatusCode -eq 200 -and -not $hasEditPosting -and $hasReviewApplicants -and $hasDeleteJob)
    Record-Test "Part 1" "Admin Details view: No Edit button" $pass "Edit Posting hidden; Review Applicants and Delete Job visible"
} catch {
    Record-Test "Part 1" "Admin Details view: No Edit button" $false $_.Exception.Message
}

# Test 1.4: Organization owner CAN still edit own job
try {
    $orgSession = Get-UserSession "red@gmail.com" "Admin@123" "Organization"
    $resp = Invoke-WebRequest -Uri "$baseUrl/Jobs/Edit/10" -WebSession $orgSession -Method Get -UseBasicParsing
    $hasForm = $resp.Content.Contains('id="editJobForm"') -or $resp.Content.Contains('action="/Jobs/Edit"')
    $pass = ($resp.StatusCode -eq 200 -and $hasForm)
    Record-Test "Part 1" "Organization can edit its own job" $pass "Status 200, Edit form rendered for job owner"
} catch {
    Record-Test "Part 1" "Organization can edit its own job" $false $_.Exception.Message
}


# ========================================================
# PART 2: SECTION 5.6 ACCESSIBILITY MAP
# ========================================================
Write-Host "`n--- Testing Part 2: Section 5.6 Accessibility Map ---" -ForegroundColor Yellow

# Test 2.1: Public Directory Listing & Features
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Accessibility" -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasTitle = $content.Contains("Accessibility Map & Places")
    $hasHospital = $content.Contains("Dhaka Medical College Hospital")
    $hasMall = $content.Contains("Bashundhara City Shopping Complex")
    $hasCoords = $content.Contains("Coords:")
    
    $pass = ($resp.StatusCode -eq 200 -and $hasTitle -and $hasHospital -and $hasMall -and $hasCoords)
    Record-Test "Part 2" "Public Accessibility Directory" $pass "Status 200, places listed with scores and coordinates"
} catch {
    Record-Test "Part 2" "Public Accessibility Directory" $false $_.Exception.Message
}

# Test 2.2: Search and Filter
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Accessibility?city=Chittagong" -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasCtgHospital = $content.Contains("Chittagong Medical College Hospital")
    $noDhaka = -not $content.Contains("Kamalapur Central Railway Station")
    
    $pass = ($resp.StatusCode -eq 200 -and $hasCtgHospital -and $noDhaka)
    Record-Test "Part 2" "Filter by City" $pass "Chittagong filter successfully isolates regional venues"
} catch {
    Record-Test "Part 2" "Filter by City" $false $_.Exception.Message
}

# Test 2.3: Place Details with Map-Ready Placeholder
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasRamp = $content.Contains("Wheelchair Ramp")
    $hasElevator = $content.Contains("Elevator")
    $hasMapReady = $content.Contains("Location Map") -and $content.Contains("Lat: 23.7258")
    
    $pass = ($resp.StatusCode -eq 200 -and $hasRamp -and $hasElevator -and $hasMapReady)
    Record-Test "Part 2" "Place Details & Map-Ready Area" $pass "Details page loaded with feature checklist and coordinate container"
} catch {
    Record-Test "Part 2" "Place Details & Map-Ready Area" $false $_.Exception.Message
}

# Test 2.4: Disability User 1 Submits Report
$user1Session = Get-UserSession "swasthy@gmail.com" "Admin@123" "Disability"
$submittedReportId = 0
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -WebSession $user1Session -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    $reportData = @{
        "__RequestVerificationToken" = $token
        "PlaceId" = "1"
        "IssueType" = "Broken Elevator"
        "Description" = "East wing elevator #2 is non-operational, causing long wait times for wheelchair users."
    }

    $postResp = Invoke-WebRequest -Uri "$baseUrl/Accessibility/SubmitReport" -WebSession $user1Session -Method Post -Body $reportData -UseBasicParsing
    
    $submittedReportId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""AccessibilityReports"" WHERE ""SubmittedByUserId"" = 1 AND ""IssueType"" = 'Broken Elevator' ORDER BY ""Id"" DESC LIMIT 1;")
    $status = Invoke-PgSql "SELECT ""Status"" FROM public.""AccessibilityReports"" WHERE ""Id"" = $submittedReportId;"

    $pass = ($submittedReportId -gt 0 -and ($status -eq "Pending" -or $status -eq "CommunityVerified" -or $status -eq "Approved"))
    Record-Test "Part 2" "Disability User Submits Report" $pass "Report #$submittedReportId created (Status='$status')"
} catch {
    Record-Test "Part 2" "Disability User Submits Report" $false $_.Exception.Message
}

# Test 2.5: Anti-Self-Verification
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -WebSession $user1Session -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    $verifyResp = Invoke-WebRequest -Uri "$baseUrl/Accessibility/VerifyReport" -WebSession $user1Session -Method Post -Body @{ "__RequestVerificationToken" = $token; "reportId" = $submittedReportId } -UseBasicParsing
    
    $count = [int](Invoke-PgSql "SELECT COUNT(*) FROM public.""AccessibilityReportVerifications"" WHERE ""AccessibilityReportId"" = $submittedReportId AND ""VerifiedByUserId"" = 1;")
    
    $pass = ($count -eq 0)
    Record-Test "Part 2" "Anti-Self-Verification Rule" $pass "Submitter cannot verify their own report (Count = $count)"
} catch {
    Record-Test "Part 2" "Anti-Self-Verification Rule" $false $_.Exception.Message
}

# Test 2.6: Community Verification by Disability User 2
$user2Session = Get-UserSession "ta12@gmail.com" "Admin@123" "Disability"
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -WebSession $user2Session -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    $verifyResp = Invoke-WebRequest -Uri "$baseUrl/Accessibility/VerifyReport" -WebSession $user2Session -Method Post -Body @{ "__RequestVerificationToken" = $token; "reportId" = $submittedReportId } -UseBasicParsing
    
    $countVerif = [int](Invoke-PgSql "SELECT COUNT(*) FROM public.""AccessibilityReportVerifications"" WHERE ""AccessibilityReportId"" = $submittedReportId AND ""VerifiedByUserId"" = 2;")
    $statusOut = Invoke-PgSql "SELECT ""Status"" FROM public.""AccessibilityReports"" WHERE ""Id"" = $submittedReportId;"
    
    $pass = ($countVerif -ge 1 -and ($statusOut -eq "CommunityVerified" -or $statusOut -eq "Approved"))
    Record-Test "Part 2" "Community Verification" $pass "Community member verified report; Verified count=$countVerif, Status='$statusOut'"
} catch {
    Record-Test "Part 2" "Community Verification" $false $_.Exception.Message
}

# Test 2.7: Anti-Duplicate Verification
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -WebSession $user2Session -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    $dupResp = Invoke-WebRequest -Uri "$baseUrl/Accessibility/VerifyReport" -WebSession $user2Session -Method Post -Body @{ "__RequestVerificationToken" = $token; "reportId" = $submittedReportId } -UseBasicParsing
    
    $count = [int](Invoke-PgSql "SELECT COUNT(*) FROM public.""AccessibilityReportVerifications"" WHERE ""AccessibilityReportId"" = $submittedReportId AND ""VerifiedByUserId"" = 2;")
    
    $pass = ($count -eq 1)
    Record-Test "Part 2" "Anti-Duplicate Verification Rule" $pass "User 2 cannot verify report a second time (Count = $count)"
} catch {
    Record-Test "Part 2" "Anti-Duplicate Verification Rule" $false $_.Exception.Message
}

# Test 2.8: Admin Approval Workflow
try {
    $adminPage = Invoke-WebRequest -Uri "$baseUrl/Admin/AccessibilityReports" -WebSession $adminSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $adminPage.Content

    $approveResp = Invoke-WebRequest -Uri "$baseUrl/Admin/ApproveAccessibilityReport" -WebSession $adminSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $submittedReportId; "adminNotes" = "Verified on-site by city engineering inspector" } -UseBasicParsing -ErrorAction SilentlyContinue
    
    $statusOut = Invoke-PgSql "SELECT ""Status"" FROM public.""AccessibilityReports"" WHERE ""Id"" = $submittedReportId;"
    
    $pass = ($statusOut -eq "Approved")
    Record-Test "Part 2" "Admin Report Approval" $pass "Admin approved report; Final Status='Approved'"
} catch {
    Record-Test "Part 2" "Admin Report Approval" $false $_.Exception.Message
}

# ========================================================
# PART 3: SECTION 5.7 VOLUNTEER MATCHING SYSTEM
# ========================================================
Write-Host "`n--- Testing Part 3: Section 5.7 Volunteer Matching System ---" -ForegroundColor Yellow

$newRequestId = 0

# Test 3.1: Disability User Creates Support Request
try {
    $createPage = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Create" -WebSession $user1Session -Method Get -UseBasicParsing
    $token = Get-VerificationToken $createPage.Content

    $dateStr = (Get-Date).AddDays(2).ToString("yyyy-MM-dd")
    $reqData = @{
        "__RequestVerificationToken" = $token
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

    $resp = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Create" -WebSession $user1Session -Method Post -Body $reqData -UseBasicParsing
    
    $newRequestId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""VolunteerSupportRequests"" WHERE ""RequestedByUserId"" = 1 ORDER BY ""Id"" DESC LIMIT 1;")
    $reqStatus = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;"
    
    $pass = ($newRequestId -gt 0 -and ($reqStatus -eq "Pending" -or $reqStatus -eq "Accepted" -or $reqStatus -eq "InProgress" -or $reqStatus -eq "Completed"))
    Record-Test "Part 3" "Create Volunteer Support Request" $pass "Support Request #$newRequestId created with Status='$reqStatus'"
} catch {
    Record-Test "Part 3" "Create Volunteer Support Request" $false $_.Exception.Message
}

# Test 3.2: Volunteer in Dhaka Sees Nearby Matching Request
$volDhakaSession = Get-UserSession "halima@gmail.com" "Admin@123" "Volunteer"
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/NearbyHelpRequests" -WebSession $volDhakaSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasRequest = $content.Contains("Assistance attending physiotherapy at CRP Savar")
    
    # Check if request exists in Nearby or was already accepted
    $reqStatus = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;"
    $pass = ($resp.StatusCode -eq 200 -and ($hasRequest -or $reqStatus -ne "Pending"))
    Record-Test "Part 3" "Location-Based Nearby Matching" $pass "Dhaka volunteer Halima matched with Dhaka support request (Status: $reqStatus)"
} catch {
    Record-Test "Part 3" "Location-Based Nearby Matching" $false $_.Exception.Message
}

# Test 3.3: Volunteer Accepts Request
try {
    $nearbyPage = Invoke-WebRequest -Uri "$baseUrl/Volunteer/NearbyHelpRequests" -WebSession $volDhakaSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $nearbyPage.Content

    $acceptResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/AcceptRequest" -WebSession $volDhakaSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "requestId" = $newRequestId } -UseBasicParsing
    
    $assignedVol = [int](Invoke-PgSql "SELECT ""AssignedVolunteerId"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;")
    $volId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""Volunteers"" WHERE ""Email"" = 'halima@gmail.com';")
    $status = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;"

    $pass = (($status -eq "Accepted" -or $status -eq "InProgress" -or $status -eq "Completed") -and $assignedVol -eq $volId)
    Record-Test "Part 3" "Volunteer Accepts Request" $pass "Request assigned to Volunteer #$assignedVol with Status='$status'"
} catch {
    Record-Test "Part 3" "Volunteer Accepts Request" $false $_.Exception.Message
}

# Test 3.4: Duplicate Acceptance Prevention
$volCtgSession = Get-UserSession "nafu@gmail.com" "Admin@123" "Volunteer"
try {
    # Get a valid token from volunteer dashboard
    $dashPage = Invoke-WebRequest -Uri "$baseUrl/Volunteer/Dashboard" -WebSession $volCtgSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $dashPage.Content

    $dupAccept = Invoke-WebRequest -Uri "$baseUrl/Volunteer/AcceptRequest" -WebSession $volCtgSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "requestId" = $newRequestId } -UseBasicParsing -ErrorAction SilentlyContinue
    
    $assignedVol = [int](Invoke-PgSql "SELECT ""AssignedVolunteerId"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;")
    $volId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""Volunteers"" WHERE ""Email"" = 'halima@gmail.com';")
    
    $pass = ($assignedVol -eq $volId)
    Record-Test "Part 3" "Duplicate Acceptance Prevention" $pass "Second volunteer prevented from claiming already accepted task (Assigned: #$assignedVol)"
} catch {
    Record-Test "Part 3" "Duplicate Acceptance Prevention" $false $_.Exception.Message
}

# Test 3.5: Task Status Update (InProgress -> Completed)
try {
    $assignedPage = Invoke-WebRequest -Uri "$baseUrl/Volunteer/AssignedTasks" -WebSession $volDhakaSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $assignedPage.Content

    # Move to InProgress
    $resp1 = Invoke-WebRequest -Uri "$baseUrl/Volunteer/UpdateTaskStatus" -WebSession $volDhakaSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "requestId" = $newRequestId; "status" = "InProgress" } -UseBasicParsing
    $s1 = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;"

    # Move to Completed
    $resp2 = Invoke-WebRequest -Uri "$baseUrl/Volunteer/UpdateTaskStatus" -WebSession $volDhakaSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "requestId" = $newRequestId; "status" = "Completed" } -UseBasicParsing
    $s2 = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $newRequestId;"

    $pass = ($s1 -eq "InProgress" -and $s2 -eq "Completed")
    Record-Test "Part 3" "Task Status Management" $pass "Task progressed Accepted -> InProgress ($s1) -> Completed ($s2)"
} catch {
    Record-Test "Part 3" "Task Status Management" $false $_.Exception.Message
}

# Test 3.6: Volunteer History
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/VolunteerHistory" -WebSession $volDhakaSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasCompleted = $content.Contains("Assistance attending physiotherapy at CRP Savar") -and $content.Contains("Completed")
    
    $pass = ($resp.StatusCode -eq 200 -and $hasCompleted)
    Record-Test "Part 3" "Volunteer Service History" $pass "Completed task archived in volunteer's service history table"
} catch {
    Record-Test "Part 3" "Volunteer Service History" $false $_.Exception.Message
}

# Test 3.7: Real-Time Chat Message Persistence & Retrieval
try {
    $chatPage = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Chat/$newRequestId" -WebSession $volDhakaSession -Method Get -UseBasicParsing
    $tokenVol = Get-VerificationToken $chatPage.Content

    # Volunteer sends message
    $msgBody1 = @{
        "__RequestVerificationToken" = $tokenVol
        "requestId" = $newRequestId
        "messageText" = "Hello Swasthy, I will meet you at the reception entrance at 9:30 AM."
    }
    $sendResp1 = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/SendMessage" -WebSession $volDhakaSession -Method Post -Body $msgBody1 -UseBasicParsing -Headers @{ "X-Requested-With" = "XMLHttpRequest" }

    # User sends reply
    $userChatPage = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Chat/$newRequestId" -WebSession $user1Session -Method Get -UseBasicParsing
    $tokenUser = Get-VerificationToken $userChatPage.Content

    $msgBody2 = @{
        "__RequestVerificationToken" = $tokenUser
        "requestId" = $newRequestId
        "messageText" = "Thank you Halima, I will see you there with my medical files."
    }
    $sendResp2 = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/SendMessage" -WebSession $user1Session -Method Post -Body $msgBody2 -UseBasicParsing -Headers @{ "X-Requested-With" = "XMLHttpRequest" }

    # Retrieve messages via API
    $getResp = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/GetMessages?requestId=$newRequestId" -WebSession $user1Session -Method Get -UseBasicParsing
    $chatList = $getResp.Content | ConvertFrom-Json

    $pass = ($chatList.Count -ge 2 -and ($chatList | Where-Object { $_.messageText -match "reception entrance" }) -and ($chatList | Where-Object { $_.messageText -match "medical files" }))
    Record-Test "Part 3" "User-Volunteer Scoped Chat" $pass "Bidirectional messages persisted and retrieved ($($chatList.Count) messages)"
} catch {
    Record-Test "Part 3" "User-Volunteer Scoped Chat" $false $_.Exception.Message
}

# ========================================================
# REGRESSION TESTING
# ========================================================
Write-Host "`n--- Testing Regression Across Existing Platform Features ---" -ForegroundColor Yellow

# Regression 4.1: Healthcare Directory
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare" -Method Get -UseBasicParsing
    $pass = ($resp.StatusCode -eq 200 -and $resp.Content.Contains("Healthcare & Therapy Finder"))
    Record-Test "Regression" "Healthcare Finder Directory" $pass "Status 200, directory functional"
} catch {
    Record-Test "Regression" "Healthcare Finder Directory" $false $_.Exception.Message
}

# Regression 4.2: Job Portal
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Jobs" -Method Get -UseBasicParsing
    $pass = ($resp.StatusCode -eq 200 -and $resp.Content.Contains("Accessible Jobs"))
    Record-Test "Regression" "Job Portal Directory" $pass "Status 200, public jobs functional"
} catch {
    Record-Test "Regression" "Job Portal Directory" $false $_.Exception.Message
}

# Regression 4.3: Dashboards
try {
    $rAdmin = (Invoke-WebRequest -Uri "$baseUrl/Admin/Dashboard" -WebSession $adminSession -Method Get -UseBasicParsing).StatusCode
    $rDisability = (Invoke-WebRequest -Uri "$baseUrl/Disability/Dashboard" -WebSession $user1Session -Method Get -UseBasicParsing).StatusCode
    $rVolunteer = (Invoke-WebRequest -Uri "$baseUrl/Volunteer/Dashboard" -WebSession $volDhakaSession -Method Get -UseBasicParsing).StatusCode
    
    $pass = ($rAdmin -eq 200 -and $rDisability -eq 200 -and $rVolunteer -eq 200)
    Record-Test "Regression" "Role Dashboards" $pass "Admin (200), Disability (200), Volunteer (200) active"
} catch {
    Record-Test "Regression" "Role Dashboards" $false $_.Exception.Message
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " TEST RUN SUMMARY" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failedTests = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host "Total Tests:  $totalTests"
Write-Host "Passed:       $passedTests" -ForegroundColor Green
Write-Host "Failed:       $failedTests" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })

if ($failedTests -gt 0) {
    Write-Host "`nFailed Tests Detail:" -ForegroundColor Red
    $testResults | Where-Object { $_.Status -eq "FAIL" } | Format-Table -AutoSize
}
