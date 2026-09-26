$baseUrl = "http://localhost:5140"

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

$disabilitySession = Get-UserSession "swasthy@gmail.com" "Admin@123" "Disability"
$aiPage = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation" -WebSession $disabilitySession -Method Get -UseBasicParsing
$token = Get-VerificationToken $aiPage.Content

Write-Host "Waiting 4s..."
Start-Sleep -Seconds 4

$resp1 = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
    "__RequestVerificationToken" = $token
    "query" = "I need a doctor or physiotherapy appointment."
} -UseBasicParsing

Write-Host "Response 1 length: $($resp1.Content.Length)"
$hasDoc = $resp1.Content -match "/Healthcare/Details/"
$hasCooldown = $resp1.Content -match "Please wait"
Write-Host "Has Healthcare Link: $hasDoc, Has Cooldown: $hasCooldown"

Write-Host "Waiting 4s..."
Start-Sleep -Seconds 4

$resp2 = Invoke-WebRequest -Uri "$baseUrl/AIRecommendation/GetRecommendations" -WebSession $disabilitySession -Method Post -Body @{
    "__RequestVerificationToken" = $token
    "query" = "astronomy rocket space shuttle astronaut flight mission"
} -UseBasicParsing

Write-Host "Response 2 length: $($resp2.Content.Length)"
$hasNoMatch = ($resp2.Content -match "No Matching Records Found" -or $resp2.Content -match "No matching opportunities" -or $resp2.Content -match "no direct matching")
$hasCooldown2 = $resp2.Content -match "Please wait"
Write-Host "Has No Match: $hasNoMatch, Has Cooldown: $hasCooldown2"
if (-not $hasNoMatch) {
    # show summary snippet
    $m = [regex]::Match($resp2.Content, 'Summary & Advice</h5>\s*<p[^>]*>(.*?)</p>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($m.Success) {
        Write-Host "Summary snippet: $($m.Groups[1].Value.Trim())"
    }
}
