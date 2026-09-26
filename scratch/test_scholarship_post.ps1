$baseUrl = "http://localhost:5140"

function Get-VerificationToken($content) {
    $match = [regex]::Match($content, 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"')
    if ($match.Success) {
        return $match.Groups[1].Value
    }
    return ""
}

# 1. Login as Disability User
$userSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $userSession -Method Get -UseBasicParsing
$token = Get-VerificationToken $loginPage.Content

$loginBody = @{
    "__RequestVerificationToken" = $token
    "Email" = "swasthy@gmail.com"
    "Password" = "Admin@123"
    "Role" = "Disability"
}
$loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $userSession -Method Post -Body $loginBody -UseBasicParsing
Write-Host "Login response status: $($loginResp.StatusCode)"

# 2. Get Scholarship ID from DB
$env:PGPASSWORD = "123456"
$scholarshipId = & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432 -d SDP1 -t -A -c "SELECT ""Id"" FROM ""Scholarships"" ORDER BY ""Id"" DESC LIMIT 1;"
$scholarshipId = $scholarshipId.Trim()
Write-Host "Found Scholarship ID: $scholarshipId"

# 3. GET Apply Page
$applyPage = Invoke-WebRequest -Uri "$baseUrl/Services/ApplyScholarship/$scholarshipId" -WebSession $userSession -Method Get -UseBasicParsing
Write-Host "GET Apply page status: $($applyPage.StatusCode)"
$applyToken = Get-VerificationToken $applyPage.Content
Write-Host "Apply token length: $($applyToken.Length)"

# 4. Prepare file and multipart post
$dummyCvPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "sample_resume.pdf")
[System.IO.File]::WriteAllBytes($dummyCvPath, [System.Text.Encoding]::UTF8.GetBytes("%PDF-1.4 Mock CV Content for Test Purposes"))

# Let's test standard Invoke-WebRequest with Form (PS 7+) or HttpWebRequest
$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"
$bodyBytes = [System.Collections.Generic.List[byte]]::new()

function Add-Field($name, $val) {
    $part = "--$boundary$LF" + "Content-Disposition: form-data; name=`"$name`"$LF$LF" + "$val$LF"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($part)
    $bodyBytes.AddRange($bytes)
}

Add-Field "__RequestVerificationToken" $applyToken
Add-Field "ScholarshipId" $scholarshipId
Add-Field "FullName" "Swasthy Tester"
Add-Field "Email" "swasthy@gmail.com"
Add-Field "Phone" "+8801711223344"
Add-Field "Address" "Dhaka, Bangladesh"
Add-Field "HighestQualification" "B.Sc. in Computer Science"
Add-Field "Institution" "Dhaka University"
Add-Field "FieldOfStudy" "Computer Science & Engineering"
Add-Field "PassingYear" "2025"
Add-Field "AcademicResult" "3.80"
Add-Field "TechnicalSkills" "C#, ASP.NET Core, JavaScript, Python, NVDA"
Add-Field "ProfessionalSkills" "Software Development, Problem Solving"
Add-Field "OtherSkills" "Research, Public Speaking"
Add-Field "Motivation" "I am deeply passionate about creating accessible software."
Add-Field "RelevantExperience" "Developed open source accessible tools."

# File Part
$fileHeader = "--$boundary$LF" + "Content-Disposition: form-data; name=`"CVFile`"; filename=`"sample_resume.pdf`"$LF" + "Content-Type: application/pdf$LF$LF"
$bodyBytes.AddRange([System.Text.Encoding]::UTF8.GetBytes($fileHeader))
$fileBytes = [System.IO.File]::ReadAllBytes($dummyCvPath)
$bodyBytes.AddRange($fileBytes)
$bodyBytes.AddRange([System.Text.Encoding]::UTF8.GetBytes("$LF--$boundary--$LF"))

$req = [System.Net.HttpWebRequest]::Create("$baseUrl/Services/ApplyScholarship")
$req.Method = "POST"
$req.ContentType = "multipart/form-data; boundary=$boundary"
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
    Write-Host "POST Response: $([int]$resp.StatusCode) $($resp.StatusDescription)"
    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $respContent = $reader.ReadToEnd()
    $reader.Close()
    $resp.Close()
    Write-Host "Response length: $($respContent.Length)"
} catch [System.Net.WebException] {
    $ex = $_.Exception
    Write-Host "WebException: $($ex.Message)"
    if ($ex.Response) {
        $httpResp = [System.Net.HttpWebResponse]$ex.Response
        Write-Host "Status Code: $([int]$httpResp.StatusCode)"
        $r = New-Object System.IO.StreamReader($httpResp.GetResponseStream())
        Write-Host "Response body: $($r.ReadToEnd().Substring(0, [System.Math]::Min(500, $r.ReadToEnd().Length)))"
    }
}
