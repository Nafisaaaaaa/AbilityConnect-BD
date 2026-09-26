$baseUrl = "http://localhost:5140"

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

function Get-UserSession($email, $password, $role = $null) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Get -UseBasicParsing
    $token = [regex]::Match($loginPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

    $body = @{
        "__RequestVerificationToken" = $token
        "Email" = $email
        "Password" = $password
    }
    if ($role) { $body["Role"] = $role }

    $null = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -Method Post -Body $body -UseBasicParsing
    return $session
}

$user1 = Get-UserSession "swasthy@gmail.com" "Admin@123" "Disability"
$user2 = Get-UserSession "ta12@gmail.com" "Admin@123" "Disability"
$admin = Get-UserSession "admin@abilityconnect.com" "Admin@123"

# 1. Submit report
$detailsPage = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -WebSession $user1 -Method Get -UseBasicParsing
$token1 = [regex]::Match($detailsPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$reportData = @{
    "__RequestVerificationToken" = $token1
    "PlaceId" = "1"
    "IssueType" = "Broken Elevator"
    "Description" = "East wing elevator #2 is non-operational, causing long wait times for wheelchair users."
}
$submitResp = Invoke-WebRequest -Uri "$baseUrl/Accessibility/SubmitReport" -WebSession $user1 -Method Post -Body $reportData -UseBasicParsing
Write-Host "Submit report URL: $($submitResp.BaseResponse.ResponseUri)"

$reportId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""AccessibilityReports"" WHERE ""SubmittedByUserId"" = 1 AND ""IssueType"" = 'Broken Elevator' ORDER BY ""Id"" DESC LIMIT 1;")
Write-Host "New Report ID: $reportId"

# 2. Anti-self verification
$verifyResp1 = Invoke-WebRequest -Uri "$baseUrl/Accessibility/VerifyReport" -WebSession $user1 -Method Post -Body @{ "__RequestVerificationToken" = $token1; "reportId" = $reportId } -UseBasicParsing
$count1 = [int](Invoke-PgSql "SELECT COUNT(*) FROM public.""AccessibilityReportVerifications"" WHERE ""AccessibilityReportId"" = $reportId AND ""VerifiedByUserId"" = 1;")
Write-Host "Anti-self verification count (should be 0): $count1"

# 3. Community verify by user 2
$detailsPage2 = Invoke-WebRequest -Uri "$baseUrl/Accessibility/Details/1" -WebSession $user2 -Method Get -UseBasicParsing
$token2 = [regex]::Match($detailsPage2.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$verifyResp2 = Invoke-WebRequest -Uri "$baseUrl/Accessibility/VerifyReport" -WebSession $user2 -Method Post -Body @{ "__RequestVerificationToken" = $token2; "reportId" = $reportId } -UseBasicParsing
$status2 = Invoke-PgSql "SELECT ""Status"" FROM public.""AccessibilityReports"" WHERE ""Id"" = $reportId;"
Write-Host "Status after user 2 verification (should be CommunityVerified): $status2"

# 4. Anti-duplicate verification
$verifyResp3 = Invoke-WebRequest -Uri "$baseUrl/Accessibility/VerifyReport" -WebSession $user2 -Method Post -Body @{ "__RequestVerificationToken" = $token2; "reportId" = $reportId } -UseBasicParsing
$count2 = [int](Invoke-PgSql "SELECT COUNT(*) FROM public.""AccessibilityReportVerifications"" WHERE ""AccessibilityReportId"" = $reportId AND ""VerifiedByUserId"" = 2;")
Write-Host "User 2 verification count (should be 1): $count2"

# 5. Admin Approve
$adminPage = Invoke-WebRequest -Uri "$baseUrl/Admin/AccessibilityReports" -WebSession $admin -Method Get -UseBasicParsing
$adminToken = [regex]::Match($adminPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$approveResp = Invoke-WebRequest -Uri "$baseUrl/Admin/ApproveAccessibilityReport" -WebSession $admin -Method Post -Body @{ "__RequestVerificationToken" = $adminToken; "id" = $reportId; "adminNotes" = "Approved on site." } -UseBasicParsing
$status3 = Invoke-PgSql "SELECT ""Status"" FROM public.""AccessibilityReports"" WHERE ""Id"" = $reportId;"
Write-Host "Status after Admin approve (should be Approved): $status3"
