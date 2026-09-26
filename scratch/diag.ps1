$env:PGPASSWORD = "123456"
$query = @"
SELECT COUNT(*) as hp_count FROM "HealthcareProviders";
SELECT "Id", "Name", "ProviderType" FROM "HealthcareProviders" LIMIT 3;
SELECT "Id", "ReporterUserId", "ReporterUserRole", "PostId", "CommentId", "Status" FROM "CommunityReports";
"@
$temp = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($temp, $query)
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -p 5432 -d SDP1 -f $temp
Remove-Item $temp -Force
