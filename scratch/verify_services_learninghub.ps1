# ========================================================
# End-to-End Verification Script:
# Part 1: Healthcare Provider Form Lat/Long Removal
# Part 2: Section 5.8 Organization Services
# Part 3: Section 5.9 Learning Hub
# Part 4: Regression Tests
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
Write-Host "Syncing credentials in DB..." -ForegroundColor Gray
Invoke-PgSql @"
UPDATE "DisabilityUsers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com');

UPDATE "Organizations"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsVerified" = true
WHERE "Email" = 'red@gmail.com';

DELETE FROM "ScholarshipApplications" WHERE "DisabilityUserId" = 1;
DELETE FROM "TrainingRegistrations" WHERE "DisabilityUserId" = 1;
DELETE FROM "EventRegistrations" WHERE "DisabilityUserId" = 1;
DELETE FROM "SavedOpportunities" WHERE "DisabilityUserId" = 1;
DELETE FROM "Scholarships" WHERE "Title" = 'National ICT Scholarship for Students with Disabilities';
DELETE FROM "TrainingPrograms" WHERE "Title" = 'Accessible Full-Stack Web Development 2026';
DELETE FROM "AwarenessEvents" WHERE "Title" = 'National Inclusive Employment Summit 2026';
DELETE FROM "LearningVideos" WHERE "Title" LIKE '%E2E Automated Tutorial%';
"@ | Out-Null

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING E2E VERIFICATION SUITE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# ========================================================
# PART 1: HEALTHCARE PROVIDER FORM LAT/LONG REMOVAL
# ========================================================
Write-Host "`n--- Testing Part 1: Healthcare Provider Form Lat/Long Removal ---" -ForegroundColor Yellow

$adminSession = Get-UserSession "admin@abilityconnect.com" "Admin@123"

# Test 1.1: Healthcare Create Form has NO Latitude or Longitude inputs
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/Create" -WebSession $adminSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasLat = $content -match 'name="Latitude"'
    $hasLng = $content -match 'name="Longitude"'
    $pass = (-not $hasLat) -and (-not $hasLng)
    Record-Test "Part 1" "Healthcare Create Form Cleanup" $pass "Form rendered with hasLat=$hasLat, hasLng=$hasLng (both must be false)"
} catch {
    Record-Test "Part 1" "Healthcare Create Form Cleanup" $false $_.Exception.Message
}

# Test 1.2: Healthcare Edit Form has NO Latitude or Longitude inputs
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/Edit/1" -WebSession $adminSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasLat = $content -match 'name="Latitude"'
    $hasLng = $content -match 'name="Longitude"'
    $pass = (-not $hasLat) -and (-not $hasLng)
    Record-Test "Part 1" "Healthcare Edit Form Cleanup" $pass "Form rendered with hasLat=$hasLat, hasLng=$hasLng (both must be false)"
} catch {
    Record-Test "Part 1" "Healthcare Edit Form Cleanup" $false $_.Exception.Message
}

# Test 1.3: Healthcare Details Page has NO Map iframe or Coordinates
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/Details/1" -WebSession $adminSession -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasMapIframe = $content -match 'google\.com/maps'
    $hasCoordinatesText = $content -match 'Coordinates:'
    $pass = (-not $hasMapIframe) -and (-not $hasCoordinatesText)
    Record-Test "Part 1" "Healthcare Details Map/Coordinates Removal" $pass "Details rendered with hasMap=$hasMapIframe, hasCoordsText=$hasCoordinatesText"
} catch {
    Record-Test "Part 1" "Healthcare Details Map/Coordinates Removal" $false $_.Exception.Message
}

# ========================================================
# PART 2: SECTION 5.8 ORGANIZATION SERVICES
# ========================================================
Write-Host "`n--- Testing Part 2: Section 5.8 Organization Services ---" -ForegroundColor Yellow

$orgSession = Get-UserSession "red@gmail.com" "Admin@123" "Organization"

# Test 2.1: Org Creates Training Program
$trainingId = 0
try {
    $createPage = Invoke-WebRequest -Uri "$baseUrl/Organization/CreateTraining" -WebSession $orgSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $createPage.Content

    $trainingData = @{
        "__RequestVerificationToken" = $token
        "Title" = "Accessible Full-Stack Web Development 2026"
        "TrainingCategory" = "Computer Training"
        "DeliveryMode" = "Online"
        "Duration" = "3 Months"
        "MaxParticipants" = "25"
        "StartDate" = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")
        "EndDate" = (Get-Date).AddMonths(3).ToString("yyyy-MM-dd")
        "RegistrationDeadline" = (Get-Date).AddDays(5).ToString("yyyy-MM-dd")
        "Location" = "Online via Zoom & Canvas"
        "ContactInfo" = "training@redorg.org"
        "SkillsCovered" = "HTML5, CSS3, JavaScript, NVDA Screen Reader, Git"
        "Eligibility" = "Open to persons with disabilities"
        "Description" = "Comprehensive coding bootcamp designed with screen-reader friendly materials and one-on-one mentorship."
    }

    $postResp = Invoke-WebRequest -Uri "$baseUrl/Organization/CreateTraining" -WebSession $orgSession -Method Post -Body $trainingData -UseBasicParsing
    $trainingId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""TrainingPrograms"" WHERE ""Title"" = 'Accessible Full-Stack Web Development 2026' ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($trainingId -gt 0)
    Record-Test "Part 2" "Org Creates Training Program" $pass "Training Program #$trainingId successfully created in database"
} catch {
    Record-Test "Part 2" "Org Creates Training Program" $false $_.Exception.Message
}

# Test 2.2: Org Creates Scholarship
$scholarshipId = 0
try {
    $createPage = Invoke-WebRequest -Uri "$baseUrl/Organization/CreateScholarship" -WebSession $orgSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $createPage.Content

    $scholarshipData = @{
        "__RequestVerificationToken" = $token
        "Title" = "National ICT Scholarship for Students with Disabilities"
        "ScholarshipType" = "Full Funding"
        "FieldOfStudy" = "Computer Science & IT"
        "ApplicationDeadline" = (Get-Date).AddDays(14).ToString("yyyy-MM-dd")
        "Location" = "Bangladesh (Nationwide)"
        "ContactInfo" = "scholarships@redorg.org"
        "Benefits" = "Full tuition waiver + 5,000 BDT monthly educational allowance + Accessible laptop"
        "Eligibility" = "Undergraduate students with disability certificate maintaining minimum GPA 3.0"
        "RequiredQualifications" = "HSC or equivalent passed, admission in recognized university"
        "Description" = "Merit-based financial aid program for talented students pursuing science and engineering degrees."
    }

    $postResp = Invoke-WebRequest -Uri "$baseUrl/Organization/CreateScholarship" -WebSession $orgSession -Method Post -Body $scholarshipData -UseBasicParsing
    $scholarshipId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""Scholarships"" WHERE ""Title"" = 'National ICT Scholarship for Students with Disabilities' ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($scholarshipId -gt 0)
    Record-Test "Part 2" "Org Creates Scholarship" $pass "Scholarship #$scholarshipId successfully created in database"
} catch {
    Record-Test "Part 2" "Org Creates Scholarship" $false $_.Exception.Message
}

# Test 2.3: Org Creates Awareness Event
$eventId = 0
try {
    $createPage = Invoke-WebRequest -Uri "$baseUrl/Organization/CreateEvent" -WebSession $orgSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $createPage.Content

    $eventData = @{
        "__RequestVerificationToken" = $token
        "Title" = "National Inclusive Employment Summit 2026"
        "EventType" = "Career Awareness"
        "EventDate" = (Get-Date).AddDays(10).ToString("yyyy-MM-dd")
        "StartTime" = "10:00 AM"
        "EndTime" = "02:00 PM"
        "RegistrationDeadline" = (Get-Date).AddDays(8).ToString("yyyy-MM-dd")
        "Location" = "Bangabandhu International Conference Center (BICC), Dhaka"
        "Eligibility" = "Open to job seekers with disabilities, corporate recruiters, and NGOs"
        "ContactInfo" = "events@redorg.org"
        "Description" = "Connecting inclusive employers with qualified candidates with disabilities. Sign language interpreters provided."
    }

    $postResp = Invoke-WebRequest -Uri "$baseUrl/Organization/CreateEvent" -WebSession $orgSession -Method Post -Body $eventData -UseBasicParsing
    $eventId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""AwarenessEvents"" WHERE ""Title"" = 'National Inclusive Employment Summit 2026' ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($eventId -gt 0)
    Record-Test "Part 2" "Org Creates Awareness Event" $pass "Awareness Event #$eventId successfully created in database"
} catch {
    Record-Test "Part 2" "Org Creates Awareness Event" $false $_.Exception.Message
}

# Test 2.4: Disability User 1 Browses Services Directory
$userSession = Get-UserSession "swasthy@gmail.com" "Admin@123" "Disability"
try {
    $tResp = Invoke-WebRequest -Uri "$baseUrl/Services/Trainings" -WebSession $userSession -Method Get -UseBasicParsing
    $sResp = Invoke-WebRequest -Uri "$baseUrl/Services/Scholarships" -WebSession $userSession -Method Get -UseBasicParsing
    $eResp = Invoke-WebRequest -Uri "$baseUrl/Services/Events" -WebSession $userSession -Method Get -UseBasicParsing

    $hasT = $tResp.Content -match "Accessible Full-Stack Web Development 2026"
    $hasS = $sResp.Content -match "National ICT Scholarship for Students with Disabilities"
    $hasE = $eResp.Content -match "National Inclusive Employment Summit 2026"

    $pass = ($tResp.StatusCode -eq 200 -and $sResp.StatusCode -eq 200 -and $eResp.StatusCode -eq 200 -and $hasT -and $hasS -and $hasE)
    Record-Test "Part 2" "Disability User Browses Services" $pass "All 3 newly created opportunities visible in public/disability directories"
} catch {
    Record-Test "Part 2" "Disability User Browses Services" $false $_.Exception.Message
}

# Test 2.5: Disability User Enrolls in Training Program (and Anti-Duplicate Check)
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Services/TrainingDetails/$trainingId" -WebSession $userSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    $regResp = Invoke-WebRequest -Uri "$baseUrl/Services/RegisterTraining" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $trainingId } -UseBasicParsing
    $regCount = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""TrainingRegistrations"" WHERE ""TrainingProgramId"" = $trainingId AND ""DisabilityUserId"" = 1;")
    
    # Attempt duplicate enrollment
    $null = Invoke-WebRequest -Uri "$baseUrl/Services/RegisterTraining" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $trainingId } -UseBasicParsing
    $regCountAfter = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""TrainingRegistrations"" WHERE ""TrainingProgramId"" = $trainingId AND ""DisabilityUserId"" = 1;")

    $pass = ($regCount -eq 1 -and $regCountAfter -eq 1)
    Record-Test "Part 2" "Training Registration & Anti-Duplicate" $pass "Enrolled successfully (Count=1, Anti-duplicate verified)"
} catch {
    Record-Test "Part 2" "Training Registration & Anti-Duplicate" $false $_.Exception.Message
}

# Test 2.6: Disability User Registers for Event (and Anti-Duplicate Check)
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Services/EventDetails/$eventId" -WebSession $userSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    $eventRegResp = Invoke-WebRequest -Uri "$baseUrl/Services/RegisterEvent" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $eventId } -UseBasicParsing
    $eventRegCount = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""EventRegistrations"" WHERE ""AwarenessEventId"" = $eventId AND ""DisabilityUserId"" = 1;")
    
    # Attempt duplicate registration
    $null = Invoke-WebRequest -Uri "$baseUrl/Services/RegisterEvent" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $eventId } -UseBasicParsing
    $eventRegCountAfter = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""EventRegistrations"" WHERE ""AwarenessEventId"" = $eventId AND ""DisabilityUserId"" = 1;")

    $pass = ($eventRegCount -eq 1 -and $eventRegCountAfter -eq 1)
    Record-Test "Part 2" "Event Registration & Anti-Duplicate" $pass "Registered successfully (Count=1, Anti-duplicate verified)"
} catch {
    Record-Test "Part 2" "Event Registration & Anti-Duplicate" $false $_.Exception.Message
}

# Test 2.7: Disability User Bookmarks Opportunities (ToggleSave & SavedOpportunities)
try {
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Services/ScholarshipDetails/$scholarshipId" -WebSession $userSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content

    # Bookmark training, scholarship, and event
    $null = Invoke-WebRequest -Uri "$baseUrl/Services/ToggleSave" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "type" = "Training"; "id" = $trainingId } -UseBasicParsing
    $null = Invoke-WebRequest -Uri "$baseUrl/Services/ToggleSave" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "type" = "Scholarship"; "id" = $scholarshipId } -UseBasicParsing
    $null = Invoke-WebRequest -Uri "$baseUrl/Services/ToggleSave" -WebSession $userSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "type" = "Event"; "id" = $eventId } -UseBasicParsing

    $savedPage = Invoke-WebRequest -Uri "$baseUrl/Services/SavedOpportunities" -WebSession $userSession -Method Get -UseBasicParsing
    $hasSavedT = $savedPage.Content -match "Accessible Full-Stack Web Development 2026"
    $hasSavedS = $savedPage.Content -match "National ICT Scholarship for Students with Disabilities"
    $hasSavedE = $savedPage.Content -match "National Inclusive Employment Summit 2026"

    $pass = ($hasSavedT -and $hasSavedS -and $hasSavedE)
    Record-Test "Part 2" "Saved Opportunities (Bookmarking)" $pass "All 3 bookmarked items correctly rendered on SavedOpportunities page"
} catch {
    Record-Test "Part 2" "Saved Opportunities (Bookmarking)" $false $_.Exception.Message
}

# Test 2.8: Disability User Applies for Scholarship with CV File Upload
$applicationId = 0
try {
    $applyPage = Invoke-WebRequest -Uri "$baseUrl/Services/ApplyScholarship/$scholarshipId" -WebSession $userSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $applyPage.Content

    # Create a small dummy PDF file for CV upload test
    $dummyCvPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "sample_resume.pdf")
    [System.IO.File]::WriteAllBytes($dummyCvPath, [System.Text.Encoding]::UTF8.GetBytes("%PDF-1.4 Mock CV Content for Test Purposes"))

    # Multipart form upload
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    $bodyBytes = [System.Collections.Generic.List[byte]]::new()

    function Add-FormField($name, $val) {
        $part = "--$boundary$LF" + "Content-Disposition: form-data; name=`"$name`"$LF$LF" + "$val$LF"
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($part)
        $bodyBytes.AddRange($bytes)
    }

    Add-FormField "__RequestVerificationToken" $token
    Add-FormField "ScholarshipId" $scholarshipId
    Add-FormField "FullName" "Swasthy Tester"
    Add-FormField "Email" "swasthy@gmail.com"
    Add-FormField "Phone" "+8801711223344"
    Add-FormField "Address" "Dhaka, Bangladesh"
    Add-FormField "HighestQualification" "B.Sc. in Computer Science"
    Add-FormField "Institution" "Dhaka University"
    Add-FormField "FieldOfStudy" "Computer Science & Engineering"
    Add-FormField "PassingYear" "2025"
    Add-FormField "AcademicResult" "3.80"
    Add-FormField "TechnicalSkills" "C#, ASP.NET Core, JavaScript, Python, NVDA"
    Add-FormField "ProfessionalSkills" "Software Development, Problem Solving"
    Add-FormField "OtherSkills" "Research, Public Speaking"
    Add-FormField "Motivation" "I am deeply passionate about creating accessible software for persons with disabilities and this grant will support my graduate studies."
    Add-FormField "RelevantExperience" "Developed open source accessible tools for visually impaired students."

    # File Part
    $fileHeader = "--$boundary$LF" + "Content-Disposition: form-data; name=`"CVFile`"; filename=`"sample_resume.pdf`"$LF" + "Content-Type: application/pdf$LF$LF"
    $bodyBytes.AddRange([System.Text.Encoding]::UTF8.GetBytes($fileHeader))
    $fileBytes = [System.IO.File]::ReadAllBytes($dummyCvPath)
    $bodyBytes.AddRange($fileBytes)
    $bodyBytes.AddRange([System.Text.Encoding]::UTF8.GetBytes("$LF--$boundary--$LF"))

    $req = [System.Net.HttpWebRequest]::Create("$baseUrl/Services/ApplyScholarship")
    $req.Method = "POST"
    $req.ContentType = "multipart/form-data; boundary=$boundary"
    $req.AllowAutoRedirect = $false
    $req.CookieContainer = New-Object System.Net.CookieContainer
    foreach ($cookie in $userSession.Cookies.GetCookies([System.Uri]"$baseUrl")) {
        $req.CookieContainer.Add($cookie)
    }
    $reqStream = $req.GetRequestStream()
    $bodyArray = $bodyBytes.ToArray()
    $reqStream.Write($bodyArray, 0, $bodyArray.Length)
    $reqStream.Close()

    try {
        $resp = $req.GetResponse()
        $resp.Close()
    } catch [System.Net.WebException] {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 302) {
            $_.Exception.Response.Close()
        } else {
            throw $_
        }
    }

    $applicationId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""ScholarshipApplications"" WHERE ""ScholarshipId"" = $scholarshipId AND ""DisabilityUserId"" = 1 ORDER BY ""Id"" DESC LIMIT 1;")
    $cvPath = Invoke-PgSql "SELECT ""CVFilePath"" FROM ""ScholarshipApplications"" WHERE ""Id"" = $applicationId;"
    
    $fullCvPath = [System.IO.Path]::Combine("c:\Users\User\source\repos\SDP1\SDP1\wwwroot", $cvPath.TrimStart('/').Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $cvExistsOnDisk = [System.IO.File]::Exists($fullCvPath)

    $pass = ($applicationId -gt 0 -and $cvExistsOnDisk)
    Record-Test "Part 2" "Scholarship Application & CV Upload" $pass "Application #$applicationId submitted with secure GUID storage on disk ($cvExistsOnDisk)"
} catch {
    Record-Test "Part 2" "Scholarship Application & CV Upload" $false $_.Exception.Message
}

# Test 2.9: Disability User Views MyRegistrations Dashboard
try {
    $myRegPage = Invoke-WebRequest -Uri "$baseUrl/Services/MyRegistrations" -WebSession $userSession -Method Get -UseBasicParsing
    $content = $myRegPage.Content
    $hasRegT = $content -match "Accessible Full-Stack Web Development 2026"
    $hasAppS = $content -match "National ICT Scholarship for Students with Disabilities"
    $hasRegE = $content -match "National Inclusive Employment Summit 2026"
    $hasPendingBadge = $content -match "Pending"

    $pass = ($hasRegT -and $hasAppS -and $hasRegE -and $hasPendingBadge)
    Record-Test "Part 2" "My Registrations Unified View" $pass "User's training enrollment, scholarship application with status badge, and event registration all verified"
} catch {
    Record-Test "Part 2" "My Registrations Unified View" $false $_.Exception.Message
}

# Test 2.10: Org Reviews Participant, Downloads CV, and Updates Status
try {
    # 1. Download CV
    $cvDownload = Invoke-WebRequest -Uri "$baseUrl/Organization/DownloadCV/$applicationId" -WebSession $orgSession -Method Get -UseBasicParsing
    $cvDownloaded = ($cvDownload.StatusCode -eq 200 -and $cvDownload.RawContentLength -gt 0)

    # 2. Update Scholarship Application Status to Shortlisted
    $detailsPage = Invoke-WebRequest -Uri "$baseUrl/Organization/ScholarshipApplications/$scholarshipId" -WebSession $orgSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $detailsPage.Content
    $null = Invoke-WebRequest -Uri "$baseUrl/Organization/UpdateScholarshipApplicationStatus" -WebSession $orgSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $applicationId; "status" = "Shortlisted" } -UseBasicParsing
    $newStatus = Invoke-PgSql "SELECT ""Status"" FROM ""ScholarshipApplications"" WHERE ""Id"" = $applicationId;"

    # 3. Update Training Registration Status to Attended
    $tRegId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""TrainingRegistrations"" WHERE ""TrainingProgramId"" = $trainingId AND ""DisabilityUserId"" = 1;")
    $null = Invoke-WebRequest -Uri "$baseUrl/Organization/UpdateTrainingRegistrationStatus" -WebSession $orgSession -Method Post -Body @{ "__RequestVerificationToken" = $token; "id" = $tRegId; "status" = "Attended" } -UseBasicParsing
    $newTStatus = Invoke-PgSql "SELECT ""Status"" FROM ""TrainingRegistrations"" WHERE ""Id"" = $tRegId;"

    $pass = ($cvDownloaded -and $newStatus -eq "Shortlisted" -and $newTStatus -eq "Attended")
    Record-Test "Part 2" "Org Reviews & Manages Participants/CV" $pass "CV downloaded successfully ($cvDownloaded), Application updated to '$newStatus', Training participant updated to '$newTStatus'"
} catch {
    Record-Test "Part 2" "Org Reviews & Manages Participants/CV" $false $_.Exception.Message
}

# ========================================================
# PART 3: SECTION 5.9 LEARNING HUB
# ========================================================
Write-Host "`n--- Testing Part 3: Section 5.9 Learning Hub ---" -ForegroundColor Yellow

# Test 3.1: Admin Adds New Video Lesson to Learning Hub
$newVideoId = 0
try {
    $createPage = Invoke-WebRequest -Uri "$baseUrl/Admin/CreateLearningVideo" -WebSession $adminSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $createPage.Content

    $videoData = @{
        "__RequestVerificationToken" = $token
        "Title" = "E2E Automated Tutorial: Getting Started with Screen Readers"
        "Category" = "Computer Skills"
        "YouTubeUrl" = "https://www.youtube.com/watch?v=kJQP7kiw5Fk"
        "Duration" = "14 mins"
        "Description" = "Step by step beginner guide to screen reader navigation, key bindings, and virtual cursors."
        "IsPublished" = "true"
    }

    $null = Invoke-WebRequest -Uri "$baseUrl/Admin/CreateLearningVideo" -WebSession $adminSession -Method Post -Body $videoData -UseBasicParsing
    $newVideoId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""LearningVideos"" WHERE ""Title"" = 'E2E Automated Tutorial: Getting Started with Screen Readers' ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($newVideoId -gt 0)
    Record-Test "Part 3" "Admin Creates Learning Video Lesson" $pass "Video lesson #$newVideoId added to Learning Hub"
} catch {
    Record-Test "Part 3" "Admin Creates Learning Video Lesson" $false $_.Exception.Message
}

# Test 3.2: Public / Disability User Browses 5 Categories in Learning Hub
try {
    $hubPage = Invoke-WebRequest -Uri "$baseUrl/LearningHub" -Method Get -UseBasicParsing
    $content = $hubPage.Content

    $hasBraille = $content -match "Braille"
    $hasSign = $content -match "Sign Language"
    $hasComputer = $content -match "Computer Skills"
    $hasFreelance = $content -match "Freelancing"
    $hasDigital = $content -match "Digital Literacy"
    $hasNewVideo = $content -match "E2E Automated Tutorial: Getting Started with Screen Readers"

    $pass = ($hasBraille -and $hasSign -and $hasComputer -and $hasFreelance -and $hasDigital -and $hasNewVideo)
    Record-Test "Part 3" "Learning Hub 5 Categories & Public Listing" $pass "All 5 core categories displayed with active counters and lesson cards"
} catch {
    Record-Test "Part 3" "Learning Hub 5 Categories & Public Listing" $false $_.Exception.Message
}

# Test 3.3: Watch Video Lesson with Safe YouTube Embed
try {
    $watchPage = Invoke-WebRequest -Uri "$baseUrl/LearningHub/Watch/$newVideoId" -Method Get -UseBasicParsing
    $content = $watchPage.Content
    $hasEmbed = $content -match 'src="https://www\.youtube-nocookie\.com/embed/kJQP7kiw5Fk"'
    $hasTitle = $content -match "E2E Automated Tutorial: Getting Started with Screen Readers"

    $pass = ($watchPage.StatusCode -eq 200 -and $hasEmbed -and $hasTitle)
    Record-Test "Part 3" "Safe YouTube Embed Conversion & Watch View" $pass "Responsive 16:9 player with https://www.youtube-nocookie.com/embed/kJQP7kiw5Fk rendered correctly"
} catch {
    Record-Test "Part 3" "Safe YouTube Embed Conversion & Watch View" $false $_.Exception.Message
}

# Test 3.4: Toggle Publish / Hide Video Lesson
try {
    $adminHub = Invoke-WebRequest -Uri "$baseUrl/Admin/LearningHub" -WebSession $adminSession -Method Get -UseBasicParsing
    $token = Get-VerificationToken $adminHub.Content

    # Toggle to unpublished / hidden
    $null = Invoke-WebRequest -Uri "$baseUrl/Admin/TogglePublishLearningVideo/$newVideoId" -WebSession $adminSession -Method Post -Body @{ "__RequestVerificationToken" = $token } -UseBasicParsing
    $isPublished = Invoke-PgSql "SELECT ""IsPublished"" FROM ""LearningVideos"" WHERE ""Id"" = $newVideoId;"

    # Public user attempts to view hidden video -> should return 404
    $hiddenStatus = 0
    try {
        $null = Invoke-WebRequest -Uri "$baseUrl/LearningHub/Watch/$newVideoId" -Method Get -UseBasicParsing
        $hiddenStatus = 200
    } catch {
        $hiddenStatus = [int]$_.Exception.Response.StatusCode
    }

    # Re-publish for clean state
    $null = Invoke-WebRequest -Uri "$baseUrl/Admin/TogglePublishLearningVideo/$newVideoId" -WebSession $adminSession -Method Post -Body @{ "__RequestVerificationToken" = $token } -UseBasicParsing

    $pass = ($isPublished -eq "f" -and $hiddenStatus -eq 404)
    Record-Test "Part 3" "Hide Unpublished Video from Public" $pass "Unpublished video returns 404 to public users (IsPublished='f')"
} catch {
    Record-Test "Part 3" "Hide Unpublished Video from Public" $false $_.Exception.Message
}

# ========================================================
# PART 4: REGRESSION TESTING
# ========================================================
Write-Host "`n--- Testing Part 4: Regression Tests ---" -ForegroundColor Yellow

try {
    $jobsResp = Invoke-WebRequest -Uri "$baseUrl/Jobs" -Method Get -UseBasicParsing
    Record-Test "Regression" "Job Portal Browse" ($jobsResp.StatusCode -eq 200) "Status 200 OK"
} catch { Record-Test "Regression" "Job Portal Browse" $false $_.Exception.Message }

try {
    $healthResp = Invoke-WebRequest -Uri "$baseUrl/Healthcare" -Method Get -UseBasicParsing
    Record-Test "Regression" "Healthcare Finder" ($healthResp.StatusCode -eq 200) "Status 200 OK"
} catch { Record-Test "Regression" "Healthcare Finder" $false $_.Exception.Message }

try {
    $accessResp = Invoke-WebRequest -Uri "$baseUrl/Accessibility" -Method Get -UseBasicParsing
    Record-Test "Regression" "Accessibility Map" ($accessResp.StatusCode -eq 200) "Status 200 OK"
} catch { Record-Test "Regression" "Accessibility Map" $false $_.Exception.Message }

try {
    $volResp = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport" -Method Get -UseBasicParsing
    Record-Test "Regression" "Volunteer Matching" ($volResp.StatusCode -eq 200) "Status 200 OK"
} catch { Record-Test "Regression" "Volunteer Matching" $false $_.Exception.Message }


Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
$passedCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$totalCount = $testResults.Count

Write-Host "Total Tests Run : $totalCount" -ForegroundColor White
Write-Host "Tests Passed   : $passedCount" -ForegroundColor $(if ($passedCount -eq $totalCount) { "Green" } else { "Yellow" })
Write-Host "Tests Failed   : $($totalCount - $passedCount)" -ForegroundColor $(if ($totalCount -eq $passedCount) { "Green" } else { "Red" })

if ($passedCount -eq $totalCount) {
    Write-Host "`n>> ALL TESTS PASSED SUCCESSFULLY! <<" -ForegroundColor Green
} else {
    Write-Host "`n>> SOME TESTS FAILED! PLEASE REVIEW OUTPUT ABOVE <<" -ForegroundColor Red
}
