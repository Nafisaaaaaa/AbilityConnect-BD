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
$volDhaka = Get-UserSession "halima@gmail.com" "Admin@123" "Volunteer"
$volCtg = Get-UserSession "nafu@gmail.com" "Admin@123" "Volunteer"

# 1. Create support request
$createPage = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Create" -WebSession $user1 -Method Get -UseBasicParsing
$createToken = [regex]::Match($createPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

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

$resp = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Create" -WebSession $user1 -Method Post -Body $reqData -UseBasicParsing
$reqId = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""VolunteerSupportRequests"" WHERE ""RequestedByUserId"" = 1 ORDER BY ""Id"" DESC LIMIT 1;")
Write-Host "Created Support Request ID: $reqId"

# 2. Nearby requests check
$nearbyResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/NearbyHelpRequests" -WebSession $volDhaka -Method Get -UseBasicParsing
$hasNearby = $nearbyResp.Content.Contains("Assistance attending physiotherapy at CRP Savar")
Write-Host "Dhaka volunteer sees request in Nearby: $hasNearby"

# 3. Volunteer accepts request
$nearbyToken = [regex]::Match($nearbyResp.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value
$acceptResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/AcceptRequest" -WebSession $volDhaka -Method Post -Body @{ "__RequestVerificationToken" = $nearbyToken; "requestId" = $reqId } -UseBasicParsing

$statusAfterAccept = Invoke-PgSql "SELECT ""Status"", ""AssignedVolunteerId"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $reqId;"
Write-Host "Status after accept: $statusAfterAccept"

# 4. Duplicate acceptance prevention
$nearbyCtg = Invoke-WebRequest -Uri "$baseUrl/Volunteer/NearbyHelpRequests" -WebSession $volCtg -Method Get -UseBasicParsing
$ctgToken = [regex]::Match($nearbyCtg.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value
$dupResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/AcceptRequest" -WebSession $volCtg -Method Post -Body @{ "__RequestVerificationToken" = $ctgToken; "requestId" = $reqId } -UseBasicParsing

$volIdHalima = [int](Invoke-PgSql "SELECT ""Id"" FROM public.""Volunteers"" WHERE ""Email"" = 'halima@gmail.com';")
$assignedAfterDup = [int](Invoke-PgSql "SELECT ""AssignedVolunteerId"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $reqId;")
Write-Host "Assigned volunteer still Halima ($volIdHalima == $assignedAfterDup): $($volIdHalima -eq $assignedAfterDup)"

# 5. Task status updates
$assignedPage = Invoke-WebRequest -Uri "$baseUrl/Volunteer/AssignedTasks" -WebSession $volDhaka -Method Get -UseBasicParsing
$assignedToken = [regex]::Match($assignedPage.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$inProgResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/UpdateTaskStatus" -WebSession $volDhaka -Method Post -Body @{ "__RequestVerificationToken" = $assignedToken; "requestId" = $reqId; "status" = "InProgress" } -UseBasicParsing
$s1 = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $reqId;"
Write-Host "Task status after InProgress: $s1"

$compResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/UpdateTaskStatus" -WebSession $volDhaka -Method Post -Body @{ "__RequestVerificationToken" = $assignedToken; "requestId" = $reqId; "status" = "Completed" } -UseBasicParsing
$s2 = Invoke-PgSql "SELECT ""Status"" FROM public.""VolunteerSupportRequests"" WHERE ""Id"" = $reqId;"
Write-Host "Task status after Completed: $s2"

# 6. Volunteer history
$historyResp = Invoke-WebRequest -Uri "$baseUrl/Volunteer/VolunteerHistory" -WebSession $volDhaka -Method Get -UseBasicParsing
$hasHistory = $historyResp.Content.Contains("Assistance attending physiotherapy at CRP Savar") -and $historyResp.Content.Contains("Completed")
Write-Host "Task present in Volunteer History: $hasHistory"

# 7. Real-time chat
$chatPageVol = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Chat/$reqId" -WebSession $volDhaka -Method Get -UseBasicParsing
$chatTokenVol = [regex]::Match($chatPageVol.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$sendMsg1 = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/SendMessage" -WebSession $volDhaka -Method Post -Body @{ "__RequestVerificationToken" = $chatTokenVol; "requestId" = $reqId; "messageText" = "Hello Swasthy, I will meet you at the reception entrance at 9:30 AM." } -UseBasicParsing -Headers @{ "X-Requested-With" = "XMLHttpRequest" }

$chatPageUser = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/Chat/$reqId" -WebSession $user1 -Method Get -UseBasicParsing
$chatTokenUser = [regex]::Match($chatPageUser.Content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"').Groups[1].Value

$sendMsg2 = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/SendMessage" -WebSession $user1 -Method Post -Body @{ "__RequestVerificationToken" = $chatTokenUser; "requestId" = $reqId; "messageText" = "Thank you Halima, I will see you there with my medical files." } -UseBasicParsing -Headers @{ "X-Requested-With" = "XMLHttpRequest" }

$getMessages = Invoke-WebRequest -Uri "$baseUrl/VolunteerSupport/GetMessages?requestId=$reqId" -WebSession $user1 -Method Get -UseBasicParsing
$chatJson = $getMessages.Content | ConvertFrom-Json
Write-Host "Chat messages retrieved count: $($chatJson.Count)"
foreach ($m in $chatJson) {
    Write-Host " - [$($m.senderRole)] $($m.senderName): $($m.messageText)"
}
