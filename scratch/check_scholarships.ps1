$env:PGPASSWORD = "123456"
$query = @"
SELECT "Id", "Title", "OrganizationId", "Status", "ApplicationDeadline" FROM "Scholarships";
SELECT "Id", "ScholarshipId", "DisabilityUserId", "Status", "CVFilePath" FROM "ScholarshipApplications";
"@
$temp = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($temp, $query)
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432 -d SDP1 -f $temp
Remove-Item $temp -Force
