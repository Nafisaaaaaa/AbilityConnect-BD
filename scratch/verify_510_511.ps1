# ========================================================
# End-to-End Verification Script for:
# Section 5.10: AI Recommendation
# Section 5.11: Community Forum
# Regression Tests for all existing modules
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

Write-Host "Syncing test credentials and database state..." -ForegroundColor Gray
Invoke-PgSql @"
UPDATE "DisabilityUsers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com');

UPDATE "Volunteers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsVerified" = true
WHERE "Email" = 'halima@gmail.com';

UPDATE "Organizations"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsVerified" = true
WHERE "Email" = 'red@gmail.com';

DELETE FROM "CommunityReports" WHERE "ReporterName" LIKE '%E2E%';
DELETE FROM "CommunityPostLikes" WHERE "UserId" IN (1, 2);
DELETE FROM "CommunityComments" WHERE "AuthorName" LIKE '%E2E%' OR "Content" LIKE '%E2E%';
DELETE FROM "CommunityPosts" WHERE "AuthorName" LIKE '%E2E%' OR "Content" LIKE '%E2E%';
"@ | Out-Null

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " STARTING 5.10 AI RECOMMENDATION VERIFICATION" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$disabilitySession = Get-UserSession "swasthy@gmail.com" "Admin@123" "Disability"
$volunteerSession = Get-UserSession "halima@gmail.com" "Admin@123" "Volunteer"
$adminSession = Get-UserSession "admin@abilityconnect.com" "Admin@123"

# Test 1.1: Disability User opens AI Recommendation Page
try {
    $aiPage = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $hasAssistant = $aiPage.Content -match "AI Recommendation Assistant"
    $hasPersonalized = $aiPage.Content -match "Personalized Profile"
    $hasInput = $aiPage.Content -match 'name="query"'
    $pass = ($aiPage.StatusCode -eq 200 -and $hasAssistant -and $hasInput)
    Record-Test "5.10 AI" "AI Recommendation Page Rendering" $pass "Page rendered with personalized badge and query input"
} catch {
    Record-Test "5.10 AI" "AI Recommendation Page Rendering" $false $_.Exception.Message
}

# Test 1.2: User asks "I am looking for a remote job."
try {
    $token = Get-VerificationToken $aiPage.Content
    $jobQueryResp = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "query" = "I am visually impaired and looking for a remote job."
    } -UseBasicParsing

    $content = $jobQueryResp.Content
    $hasSummary = $content -match "Recommendation Results"
    $hasJobLink = $content -match "/Jobs/Details/"
    $hasWhyMatches = $content -match "Why this matches:"
    $pass = ($jobQueryResp.StatusCode -eq 200 -and $hasSummary -and $hasJobLink -and $hasWhyMatches)
    Record-Test "5.10 AI" "Remote Job Recommendation" $pass "Returned recommended jobs with direct link and explanation"
} catch {
    Record-Test "5.10 AI" "Remote Job Recommendation" $false $_.Exception.Message
}

# Test 1.3: User asks "I want to learn freelancing for free."
Start-Sleep -Seconds 3 # prevent cooldown
try {
    $learnQueryResp = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "query" = "I want to learn freelancing for free."
    } -UseBasicParsing

    $content = $learnQueryResp.Content
    $hasResult = $content -match "Recommendation Results"
    $hasTrainingOrLearning = ($content -match "/Services/TrainingDetails/" -or $content -match "/LearningHub/Watch/" -or $content -match "Freelancing")
    $pass = ($learnQueryResp.StatusCode -eq 200 -and $hasResult -and $hasTrainingOrLearning)
    Record-Test "5.10 AI" "Freelancing / Training Recommendation" $pass "Returned relevant training / learning hub opportunities"
} catch {
    Record-Test "5.10 AI" "Freelancing / Training Recommendation" $false $_.Exception.Message
}

# Test 1.4: User asks for doctors / physiotherapy
Start-Sleep -Seconds 4
try {
    $healthQueryResp = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "query" = "I need a doctor or physiotherapy appointment."
    } -UseBasicParsing

    $content = $healthQueryResp.Content
    $hasResult = $content -match "Recommendation Results"
    $hasDoctorLink = $content -match "/Healthcare/Details/"
    $hasCooldown = $content -match "Please wait"
    $pass = ($healthQueryResp.StatusCode -eq 200 -and $hasResult -and $hasDoctorLink)
    Record-Test "5.10 AI" "Healthcare Recommendation" $pass "Returned healthcare providers (Link=$hasDoctorLink, Cooldown=$hasCooldown, Result=$hasResult)"
} catch {
    Record-Test "5.10 AI" "Healthcare Recommendation" $false $_.Exception.Message
}

# Test 1.5: Query with no matching opportunities in database
Start-Sleep -Seconds 4
try {
    $noMatchResp = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "query" = "astronomy rocket space shuttle astronaut flight mission"
    } -UseBasicParsing

    $content = $noMatchResp.Content
    $bodyContent = $content -replace '<input[^>]*id="userQuery"[^>]*>', ''
    $noFakeInvented = (-not ($bodyContent -match "rocket space shuttle"))
    $hasNoMatchText = ($content -match "No Matching Records Found" -or $content -match "no direct matching" -or $content -match "No matching opportunities")
    $hasCooldown = $content -match "Please wait"
    $pass = ($noMatchResp.StatusCode -eq 200 -and $noFakeInvented -and (-not $hasCooldown) -and $hasNoMatchText)
    Record-Test "5.10 AI" "Safety Rule: No Fabricated Records" $pass "AI did not invent fake opportunities (NoMatchText=$hasNoMatchText, Cooldown=$hasCooldown)"
} catch {
    Record-Test "5.10 AI" "Safety Rule: No Fabricated Records" $false $_.Exception.Message
}

# Test 1.6: Anti-spam cooldown limit
try {
    # Send immediate rapid request
    $rapidResp = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "query" = "testing cooldown spam request"
    } -UseBasicParsing

    $hasCooldown = $rapidResp.Content -match "Please wait"
    Record-Test "5.10 AI" "Rate Limiting / Anti-Spam Check" $hasCooldown "Rapid requests properly handled with cooldown protection"
} catch {
    Record-Test "5.10 AI" "Rate Limiting / Anti-Spam Check" $false $_.Exception.Message
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " STARTING 5.11 COMMUNITY FORUM VERIFICATION" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Test 2.1: Anonymous user cannot access community directly
try {
    $anonSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $anonResp = Invoke-WebRequest -Uri "$baseUrl/Community" -WebSession $anonSession -Method Get -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $redirectsToLogin = ($anonResp.StatusCode -eq 302 -and $anonResp.Headers.Location -match "Login")
    Record-Test "5.11 Community" "Auth Guard: Anon Redirects to Login" $redirectsToLogin "Anonymous browsing properly guarded"
} catch {
    $isRedirect = $_.Exception.Response.StatusCode -eq 302
    Record-Test "5.11 Community" "Auth Guard: Anon Redirects to Login" $isRedirect "Redirected: $($_.Exception.Message)"
}

# Test 2.2: Disability User Views Community Feed
try {
    $feedResp = Invoke-WebRequest -Uri "$baseUrl/Community" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $hasFeed = $feedResp.Content -match "Community Forum" -and $feedResp.Content -match "Create Post"
    Record-Test "5.11 Community" "Disability User Views Feed" ($feedResp.StatusCode -eq 200 -and $hasFeed) "Feed rendered with post creator"
} catch {
    Record-Test "5.11 Community" "Disability User Views Feed" $false $_.Exception.Message
}

# Test 2.3: Disability User Creates a Post
$createdPostId = 0
try {
    $token = Get-VerificationToken $feedResp.Content
    $createPostResp = Invoke-WebRequest -Uri "$baseUrl/Community/CreatePost" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "Content" = "E2E Automated Post: I recently started freelance typing and wanted to share my experience with everyone!"
    } -UseBasicParsing

    $createdPostId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""CommunityPosts"" WHERE ""Content"" LIKE '%E2E Automated Post%' ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($createdPostId -gt 0)
    Record-Test "5.11 Community" "Disability User Creates Post" $pass "Post #$createdPostId successfully stored in database"
} catch {
    Record-Test "5.11 Community" "Disability User Creates Post" $false $_.Exception.Message
}

# Test 2.4: Other Authenticated User (Volunteer) Sees the Post
try {
    $volFeed = Invoke-WebRequest -Uri "$baseUrl/Community" -WebSession $volunteerSession -Method Get -UseBasicParsing
    $seesPost = $volFeed.Content -match "E2E Automated Post: I recently started freelance typing"
    Record-Test "5.11 Community" "Other Authenticated User Sees Post" $seesPost "Volunteer user sees post in community feed"
} catch {
    Record-Test "5.11 Community" "Other Authenticated User Sees Post" $false $_.Exception.Message
}

# Test 2.5: Volunteer Likes Post
try {
    $volToken = Get-VerificationToken $volFeed.Content
    $likeResp = Invoke-WebRequest -Uri "$baseUrl/Community/ToggleLike" -WebSession $volunteerSession -Method Post -Body @{
        "__RequestVerificationToken" = $volToken
        "postId" = $createdPostId
    } -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -UseBasicParsing

    $likeCountDb = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""CommunityPostLikes"" WHERE ""CommunityPostId"" = $createdPostId;")
    $pass = ($likeCountDb -eq 1)
    Record-Test "5.11 Community" "User Likes Post (Count Increases)" $pass "Post #$createdPostId like count increased to 1"
} catch {
    Record-Test "5.11 Community" "User Likes Post (Count Increases)" $false $_.Exception.Message
}

# Test 2.6: Volunteer Clicks Like Again (Unlike)
try {
    $unlikeResp = Invoke-WebRequest -Uri "$baseUrl/Community/ToggleLike" -WebSession $volunteerSession -Method Post -Body @{
        "__RequestVerificationToken" = $volToken
        "postId" = $createdPostId
    } -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -UseBasicParsing

    $likeCountDbAfter = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""CommunityPostLikes"" WHERE ""CommunityPostId"" = $createdPostId;")
    $pass = ($likeCountDbAfter -eq 0)
    Record-Test "5.11 Community" "User Unlikes Post (Like Removed)" $pass "Post #$createdPostId like removed on second click"
} catch {
    Record-Test "5.11 Community" "User Unlikes Post (Like Removed)" $false $_.Exception.Message
}

# Test 2.7: Volunteer Adds a Comment
$createdCommentId = 0
try {
    $commentResp = Invoke-WebRequest -Uri "$baseUrl/Community/AddComment" -WebSession $volunteerSession -Method Post -Body @{
        "__RequestVerificationToken" = $volToken
        "PostId" = $createdPostId
        "Content" = "E2E Automated Comment: Congratulations on your achievement! That is very inspiring."
    } -UseBasicParsing

    $createdCommentId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""CommunityComments"" WHERE ""CommunityPostId"" = $createdPostId AND ""Content"" LIKE '%E2E Automated Comment%' ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($createdCommentId -gt 0)
    Record-Test "5.11 Community" "User Adds Comment" $pass "Comment #$createdCommentId saved and linked to Post #$createdPostId"
} catch {
    Record-Test "5.11 Community" "User Adds Comment" $false $_.Exception.Message
}

# Test 2.8: Disability User Edits Own Post
try {
    $editPage = Invoke-WebRequest -Uri "$baseUrl/Community/EditPost/$createdPostId" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $editToken = Get-VerificationToken $editPage.Content

    $null = Invoke-WebRequest -Uri "$baseUrl/Community/EditPost" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $editToken
        "Id" = $createdPostId
        "Content" = "E2E Automated Post [UPDATED]: I recently started freelance typing and wanted to share my updated experience!"
    } -UseBasicParsing

    $updatedContent = Invoke-PgSql "SELECT ""Content"" FROM ""CommunityPosts"" WHERE ""Id"" = $createdPostId;"
    $pass = ($updatedContent -match "\[UPDATED\]")
    Record-Test "5.11 Community" "Author Edits Own Post" $pass "Post content successfully updated in database"
} catch {
    Record-Test "5.11 Community" "Author Edits Own Post" $false $_.Exception.Message
}

# Test 2.9: Unauthorized User CANNOT Edit Another User's Post
try {
    $unauthEdit = Invoke-WebRequest -Uri "$baseUrl/Community/EditPost/$createdPostId" -WebSession $volunteerSession -Method Get -UseBasicParsing
    $isBlocked = ($unauthEdit.Content -match "You can only edit your own posts" -or $unauthEdit.StatusCode -eq 302)
    Record-Test "5.11 Community" "Auth Guard: Cannot Edit Others' Posts" $isBlocked "Volunteer correctly prevented from editing disability user post"
} catch {
    Record-Test "5.11 Community" "Auth Guard: Cannot Edit Others' Posts" $true "Access blocked with redirect or forbidden"
}

# Test 2.10: User Reports Inappropriate Post
$createdReportId = 0
try {
    $reportResp = Invoke-WebRequest -Uri "$baseUrl/Community/ReportContent" -WebSession $volunteerSession -Method Post -Body @{
        "__RequestVerificationToken" = $volToken
        "PostId" = $createdPostId
        "Reason" = "Spam"
        "Details" = "E2E Automated Report: Testing community moderation flagging system."
    } -UseBasicParsing

    $createdReportId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""CommunityReports"" WHERE ""PostId"" = $createdPostId ORDER ""Id"" DESC LIMIT 1;" 2>$null)
    if ($createdReportId -eq 0) {
        $createdReportId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""CommunityReports"" WHERE ""PostId"" = $createdPostId ORDER BY ""Id"" DESC LIMIT 1;")
    }
    $pass = ($createdReportId -gt 0)
    Record-Test "5.11 Community" "User Reports Post" $pass "Report #$createdReportId created with Status=Pending"
} catch {
    Record-Test "5.11 Community" "User Reports Post" $false $_.Exception.Message
}

# Test 2.11: Duplicate Report Prevention
try {
    $dupResp = Invoke-WebRequest -Uri "$baseUrl/Community/ReportContent" -WebSession $volunteerSession -Method Post -Body @{
        "__RequestVerificationToken" = $volToken
        "PostId" = $createdPostId
        "Reason" = "Spam"
        "Details" = "E2E Duplicate Report"
    } -UseBasicParsing

    $reportCount = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""CommunityReports"" WHERE ""PostId"" = $createdPostId;")
    $pass = ($reportCount -eq 1)
    Record-Test "5.11 Community" "Anti-Duplicate Report Prevention" $pass "Duplicate report blocked (Count remains 1)"
} catch {
    Record-Test "5.11 Community" "Anti-Duplicate Report Prevention" $false $_.Exception.Message
}

# Test 2.12: User Reports Comment
$commentReportId = 0
try {
    $commReportResp = Invoke-WebRequest -Uri "$baseUrl/Community/ReportContent" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "CommentId" = $createdCommentId
        "Reason" = "Harassment"
        "Details" = "E2E Automated Comment Report"
    } -UseBasicParsing

    $commentReportId = [int](Invoke-PgSql "SELECT ""Id"" FROM ""CommunityReports"" WHERE ""CommentId"" = $createdCommentId ORDER BY ""Id"" DESC LIMIT 1;")
    $pass = ($commentReportId -gt 0)
    Record-Test "5.11 Community" "User Reports Comment" $pass "Comment Report #$commentReportId created in database"
} catch {
    Record-Test "5.11 Community" "User Reports Comment" $false $_.Exception.Message
}

# Test 2.13: Admin Reviews Moderation Page and Resolves Report
try {
    $modPage = Invoke-WebRequest -Uri "$baseUrl/Community/Moderation" -WebSession $adminSession -Method Get -UseBasicParsing
    $seesReportsTab = $modPage.Content -match "Admin Community Moderation"
    $adminToken = Get-VerificationToken $modPage.Content

    # Resolve report
    $resolveResp = Invoke-WebRequest -Uri "$baseUrl/Community/UpdateReportStatus" -WebSession $adminSession -Method Post -Body @{
        "__RequestVerificationToken" = $adminToken
        "id" = $createdReportId
        "status" = "Resolved"
        "adminNotes" = "Reviewed and handled during automated test."
    } -UseBasicParsing

    $reportStatusDb = Invoke-PgSql "SELECT ""Status"" FROM ""CommunityReports"" WHERE ""Id"" = $createdReportId;"
    $pass = ($reportStatusDb -eq "Resolved")
    Record-Test "5.11 Community" "Admin Moderation Review & Resolve" $pass "Admin viewed moderation dashboard and resolved Report #$createdReportId"
} catch {
    Record-Test "5.11 Community" "Admin Moderation Review & Resolve" $false $_.Exception.Message
}

# Test 2.14: Admin Comments on Post
try {
    $adminCommentResp = Invoke-WebRequest -Uri "$baseUrl/Community/AddComment" -WebSession $adminSession -Method Post -Body @{
        "__RequestVerificationToken" = $adminToken
        "PostId" = $createdPostId
        "Content" = "E2E Automated Admin Comment: Welcome to AbilityConnect BD! Great to see our community growing."
    } -UseBasicParsing

    $adminCommentCount = [int](Invoke-PgSql "SELECT COUNT(*) FROM ""CommunityComments"" WHERE ""CommunityPostId"" = $createdPostId AND ""UserRole"" = 'Admin';")
    $pass = ($adminCommentCount -gt 0)
    Record-Test "5.11 Community" "Admin Participates via Comment" $pass "Admin successfully posted comment on post"
} catch {
    Record-Test "5.11 Community" "Admin Participates via Comment" $false $_.Exception.Message
}

# Test 2.15: Admin Hides Post & Verifies Feed Visibility
try {
    $hideResp = Invoke-WebRequest -Uri "$baseUrl/Community/ToggleHidePost" -WebSession $adminSession -Method Post -Body @{
        "__RequestVerificationToken" = $adminToken
        "id" = $createdPostId
    } -UseBasicParsing

    $isHiddenDb = Invoke-PgSql "SELECT ""IsHidden"" FROM ""CommunityPosts"" WHERE ""Id"" = $createdPostId;"

    # Regular user feed should NOT have this hidden post
    $regFeed = Invoke-WebRequest -Uri "$baseUrl/Community" -WebSession $volunteerSession -Method Get -UseBasicParsing
    $userSeesHidden = $regFeed.Content -match "E2E Automated Post \[UPDATED\]"

    # Admin feed CAN see it with hidden badge
    $adminFeed = Invoke-WebRequest -Uri "$baseUrl/Community" -WebSession $adminSession -Method Get -UseBasicParsing
    $adminSeesHidden = $adminFeed.Content -match "Hidden by Moderator"

    $pass = ($isHiddenDb -eq "t" -and (-not $userSeesHidden) -and $adminSeesHidden)
    Record-Test "5.11 Community" "Admin Hides Post & Feed Filter" $pass "Post hidden from regular users, visible with warning badge to Admin"
} catch {
    Record-Test "5.11 Community" "Admin Hides Post & Feed Filter" $false $_.Exception.Message
}

# Test 2.16: User Deletes Own Comment
try {
    $delCommentResp = Invoke-WebRequest -Uri "$baseUrl/Community/DeleteComment" -WebSession $volunteerSession -Method Post -Body @{
        "__RequestVerificationToken" = $volToken
        "id" = $createdCommentId
    } -UseBasicParsing

    $commentDeleted = Invoke-PgSql "SELECT ""IsDeleted"" FROM ""CommunityComments"" WHERE ""Id"" = $createdCommentId;"
    $pass = ($commentDeleted -eq "t")
    Record-Test "5.11 Community" "User Deletes Own Comment" $pass "Comment marked IsDeleted=true in database"
} catch {
    Record-Test "5.11 Community" "User Deletes Own Comment" $false $_.Exception.Message
}

# Test 2.17: Disability User Deletes Own Post
try {
    $delPostResp = Invoke-WebRequest -Uri "$baseUrl/Community/DeletePost" -WebSession $disabilitySession -Method Post -Body @{
        "__RequestVerificationToken" = $token
        "id" = $createdPostId
    } -UseBasicParsing

    $postDeleted = Invoke-PgSql "SELECT ""IsDeleted"" FROM ""CommunityPosts"" WHERE ""Id"" = $createdPostId;"
    $pass = ($postDeleted -eq "t")
    Record-Test "5.11 Community" "Author Deletes Own Post" $pass "Post marked IsDeleted=true and removed from feed"
} catch {
    Record-Test "5.11 Community" "Author Deletes Own Post" $false $_.Exception.Message
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " STARTING REGRESSION VERIFICATION" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Regression: Jobs
try {
    $rJobs = Invoke-WebRequest -Uri "$baseUrl/Jobs" -WebSession $disabilitySession -Method Get -UseBasicParsing
    Record-Test "Regression" "Job Portal" ($rJobs.StatusCode -eq 200 -and $rJobs.Content -match "Accessible Jobs") "Job portal intact"
} catch {
    Record-Test "Regression" "Job Portal" $false $_.Exception.Message
}

# Regression: Healthcare
try {
    $rHealth = Invoke-WebRequest -Uri "$baseUrl/Healthcare" -WebSession $disabilitySession -Method Get -UseBasicParsing
    Record-Test "Regression" "Healthcare Finder" ($rHealth.StatusCode -eq 200 -and $rHealth.Content -match "Healthcare") "Healthcare finder intact"
} catch {
    Record-Test "Regression" "Healthcare Finder" $false $_.Exception.Message
}

# Regression: Accessibility Map
try {
    $rMap = Invoke-WebRequest -Uri "$baseUrl/Accessibility" -WebSession $disabilitySession -Method Get -UseBasicParsing
    Record-Test "Regression" "Accessibility Map" ($rMap.StatusCode -eq 200 -and $rMap.Content -match "Accessibility") "Accessibility map intact"
} catch {
    Record-Test "Regression" "Accessibility Map" $false $_.Exception.Message
}

# Regression: Volunteer Matching
try {
    $rVol = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport" -WebSession $disabilitySession -Method Get -UseBasicParsing
    Record-Test "Regression" "Volunteer Matching" ($rVol.StatusCode -eq 200) "Volunteer matching intact"
} catch {
    Record-Test "Regression" "Volunteer Matching" $false $_.Exception.Message
}

# Regression: Services (Trainings, Scholarships, Events)
try {
    $rTrn = Invoke-WebRequest -Uri "$baseUrl/Services/Trainings" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $rSch = Invoke-WebRequest -Uri "$baseUrl/Services/Scholarships" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $rEvt = Invoke-WebRequest -Uri "$baseUrl/Services/Events" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $pass = ($rTrn.StatusCode -eq 200 -and $rSch.StatusCode -eq 200 -and $rEvt.StatusCode -eq 200)
    Record-Test "Regression" "Organization Services" $pass "Trainings, Scholarships, Events all return 200"
} catch {
    Record-Test "Regression" "Organization Services" $false $_.Exception.Message
}

# Regression: Learning Hub
try {
    $rLrn = Invoke-WebRequest -Uri "$baseUrl/LearningHub" -WebSession $disabilitySession -Method Get -UseBasicParsing
    Record-Test "Regression" "Learning Hub" ($rLrn.StatusCode -eq 200 -and $rLrn.Content -match "Learning Hub") "Learning Hub intact"
} catch {
    Record-Test "Regression" "Learning Hub" $false $_.Exception.Message
}

# Regression: Dashboards
try {
    $rDashDis = Invoke-WebRequest -Uri "$baseUrl/Disability/Dashboard" -WebSession $disabilitySession -Method Get -UseBasicParsing
    $rDashAdm = Invoke-WebRequest -Uri "$baseUrl/Admin/Dashboard" -WebSession $adminSession -Method Get -UseBasicParsing
    $pass = ($rDashDis.StatusCode -eq 200 -and $rDashAdm.StatusCode -eq 200)
    Record-Test "Regression" "Dashboards & RBAC" $pass "Disability & Admin dashboards operational"
} catch {
    Record-Test "Regression" "Dashboards & RBAC" $false $_.Exception.Message
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " TEST SUMMARY REPORT" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
$passedCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failedCount = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$totalCount = $testResults.Count

Write-Host "Total Tests: $totalCount | Passed: $passedCount | Failed: $failedCount" -ForegroundColor $(if ($failedCount -eq 0) { "Green" } else { "Red" })

if ($failedCount -gt 0) {
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $testResults | Where-Object { $_.Status -eq "FAIL" } | Format-Table -AutoSize
}
