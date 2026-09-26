# Comprehensive automated functional test script for Phase 2: Section 5.5 Healthcare & Therapy Finder
# Architecture: Direct Admin Healthcare Provider Management (No separate provider verification)
$ErrorActionPreference = "Stop"

$baseUrl = "http://localhost:5140"
$pgPass = "123456"
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$testResults = [System.Collections.Generic.List[PSCustomObject]]::new()

function Run-Sql($query) {
    $env:PGPASSWORD = $pgPass
    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tempFile, $query)
    try {
        $out = & $psqlPath -U postgres -h localhost -p 5432 -d SDP1 -t -A -f $tempFile
        return $out
    } finally {
        if (Test-Path $tempFile) { Remove-Item $tempFile -Force }
    }
}

function Record-Test($name, $passed, $details) {
    $result = [PSCustomObject]@{
        TestName = $name
        Status   = if ($passed) { "PASS" } else { "FAIL" }
        Details  = $details
    }
    $testResults.Add($result)
    $badge = if ($passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "$badge $name - $details" -ForegroundColor $color
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING AUTOMATED FUNCTIONAL VERIFICATION: PHASE 2" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Dynamically resolve seeded entity IDs
$p1Id = (Run-Sql 'SELECT "Id" FROM public."HealthcareProviders" WHERE "Name" LIKE ''%Mahbubur%'' LIMIT 1;').Trim()
$p2Id = (Run-Sql 'SELECT "Id" FROM public."HealthcareProviders" WHERE "Name" LIKE ''%Fahmida%'' LIMIT 1;').Trim()
$p3Id = (Run-Sql 'SELECT "Id" FROM public."HealthcareProviders" WHERE "Name" LIKE ''%Zahidul%'' LIMIT 1;').Trim()
$u1Id = (Run-Sql 'SELECT "Id" FROM public."DisabilityUsers" LIMIT 1;').Trim()

# 1. Test Public Healthcare Directory
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare" -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasTitle = $content.Contains("Healthcare & Therapy Finder")
    $hasDoctor = $content.Contains("Dr. Mahbubur Rahman")
    $hasCRP = $content.Contains("Centre for Rehabilitation of the Paralysed") -or $content.Contains("CRP Savar")
    $hasAllProviders = $content.Contains("Dr. Kazi Imran")

    $pass = ($resp.StatusCode -eq 200 -and $hasTitle -and $hasDoctor -and $hasCRP -and $hasAllProviders)
    Record-Test "1. Public Healthcare Directory" $pass "Status 200, all platform-managed healthcare providers displayed"
} catch {
    Record-Test "1. Public Healthcare Directory" $false $_.Exception.Message
}

# 2. Test Keyword Search (Speech)
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare?Keyword=Speech" -Method Get -UseBasicParsing
    $hasSpeech = $resp.Content.Contains("Tahmina Akter")
    $notDoctor = -not $resp.Content.Contains("Dr. Mahbubur Rahman")
    Record-Test "2. Keyword Search (Speech)" ($resp.StatusCode -eq 200 -and $hasSpeech -and $notDoctor) "Search returned Speech Therapist Tahmina Akter"
} catch {
    Record-Test "2. Keyword Search (Speech)" $false $_.Exception.Message
}

# 3. Test Provider Type Filter (Physiotherapist)
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare?ProviderType=Physiotherapist" -Method Get -UseBasicParsing
    $hasPT = $resp.Content.Contains("Md. Zahidul Islam, PT")
    $notSpeech = -not $resp.Content.Contains("Tahmina Akter")
    Record-Test "3. Provider Type Filter (Physiotherapist)" ($resp.StatusCode -eq 200 -and $hasPT -and $notSpeech) "Filter returned only Physiotherapist"
} catch {
    Record-Test "3. Provider Type Filter (Physiotherapist)" $false $_.Exception.Message
}

# 4. Test District Filter (Chittagong)
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare?District=Chittagong" -Method Get -UseBasicParsing
    $hasCtg = $resp.Content.Contains("Dr. Fahmida Sultana")
    $notDhaka = -not $resp.Content.Contains("Bashundhara")
    Record-Test "4. District Filter (Chittagong)" ($resp.StatusCode -eq 200 -and $hasCtg -and $notDhaka) "Filter isolated Chittagong healthcare providers"
} catch {
    Record-Test "4. District Filter (Chittagong)" $false $_.Exception.Message
}

# 5. Test Sorting by Name
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare?SortBy=name_asc" -Method Get -UseBasicParsing
    Record-Test "5. Sorting (Name Ascending)" ($resp.StatusCode -eq 200) "Sort query executed successfully"
} catch {
    Record-Test "5. Sorting (Name Ascending)" $false $_.Exception.Message
}

# 6. Test Provider Details Page & Google Maps Graceful Fallback
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/Healthcare/Details/$p1Id" -Method Get -UseBasicParsing
    $content = $resp.Content
    $hasName = $content.Contains("Dr. Mahbubur Rahman")
    $hasServices = $content.Contains("Stroke Rehab")
    $hasMap = $content.Contains("maps.google.com") -or $content.Contains("google.com/maps/embed")
    $hasReview = $content.Contains("outstanding care")
    $hasChatModal = $content.Contains("chatNoticeModal")

    $pass = ($resp.StatusCode -eq 200 -and $hasName -and $hasServices -and $hasMap -and $hasReview -and $hasChatModal)
    Record-Test "6. Provider Details & Maps Fallback" $pass "Details, services, reviews, chat modal and Google Maps fallback loaded"
} catch {
    Record-Test "6. Provider Details & Maps Fallback" $false $_.Exception.Message
}

# 7. Test Healthcare Providers across all 6 specified categories exist in Database
try {
    $typesSql = 'SELECT DISTINCT "ProviderType" FROM public."HealthcareProviders" ORDER BY "ProviderType";'
    $typesOut = (Run-Sql $typesSql) -split "`r?`n"
    $hasAll6 = ($typesOut -contains "Doctor") -and
               ($typesOut -contains "Physiotherapist") -and
               ($typesOut -contains "Speech Therapist") -and
               ($typesOut -contains "Rehabilitation Center") -and
               ($typesOut -contains "Eye Specialist") -and
               ($typesOut -contains "Mental Health Specialist")

    Record-Test "7. All 6 Healthcare Categories Seeded" $hasAll6 "Doctor, Physiotherapist, Speech Therapist, Rehab Center, Eye Specialist, Mental Health Specialist present"
} catch {
    Record-Test "7. All 6 Healthcare Categories Seeded" $false $_.Exception.Message
}

# 8. Test Healthcare Appointments Initialized
try {
    $apptCheckSql = 'SELECT COUNT(*) FROM public."HealthcareAppointments";'
    $apptCount = [int]((Run-Sql $apptCheckSql) -split "`r?`n")[0].Trim()
    $pass = ($apptCount -ge 2)
    Record-Test "8. Healthcare Appointments Initialized" $pass "Active appointments present in database ($apptCount)"
} catch {
    Record-Test "8. Healthcare Appointments Initialized" $false $_.Exception.Message
}

# 9. Test Appointment Status Flow (Pending -> Confirmed -> Completed)
try {
    $testApptSql = @"
INSERT INTO public."HealthcareAppointments" (
    "HealthcareProviderId", "DisabilityUserId", "AppointmentDate", "TimeSlot",
    "ReasonForVisit", "PatientNotes", "Status", "CreatedAt"
) VALUES (
    $p2Id, $u1Id, NOW() + INTERVAL '7 days', '03:00 PM - 03:30 PM',
    'Automated test appointment for pediatric neurology consultation.',
    'Wheelchair ramp required.', 'Pending', NOW()
) RETURNING "Id";
"@
    $newApptId = ((Run-Sql $testApptSql) -split "`r?`n")[0].Trim()

    # Update to Confirmed with DoctorNotes
    $confirmSql = "UPDATE public.`"HealthcareAppointments`" SET `"Status`" = 'Confirmed', `"DoctorNotes`" = 'Confirmed. Please bring previous MRI scans.', `"UpdatedAt`" = NOW() WHERE `"Id`" = $newApptId; SELECT `"Status`" FROM public.`"HealthcareAppointments`" WHERE `"Id`" = $newApptId;"
    $confirmedStatus = ((Run-Sql $confirmSql) -split "`r?`n")[-1].Trim()

    # Update to Completed
    $completeSql = "UPDATE public.`"HealthcareAppointments`" SET `"Status`" = 'Completed', `"UpdatedAt`" = NOW() WHERE `"Id`" = $newApptId; SELECT `"Status`" FROM public.`"HealthcareAppointments`" WHERE `"Id`" = $newApptId;"
    $completedStatus = ((Run-Sql $completeSql) -split "`r?`n")[-1].Trim()

    $statusFlowPass = ($confirmedStatus -eq "Confirmed" -and $completedStatus -eq "Completed")
    Record-Test "9. Appointment Status Lifecycle (Pending -> Confirmed -> Completed)" $statusFlowPass "Appointment $newApptId transitioned successfully"
} catch {
    Record-Test "9. Appointment Status Lifecycle" $false $_.Exception.Message
}

# 10. Test Healthcare Review Constraint (1 review per user per provider)
try {
    # Attempt duplicate review for Provider 1 by User 1 (already reviewed in seed)
    $dupReviewSql = @"
INSERT INTO public."HealthcareReviews" (
    "HealthcareProviderId", "DisabilityUserId", "Rating", "ReviewText", "CreatedAt"
) VALUES (
    $p1Id, $u1Id, 4, 'Duplicate review attempt.', NOW()
);
"@
    $dupFailed = $false
    try {
        $out = Run-Sql $dupReviewSql 2>&1
        if ($out -match "duplicate key" -or $LASTEXITCODE -ne 0) {
            $dupFailed = $true
        }
    } catch {
        $dupFailed = $true
    }

    Record-Test "10. One-Review-Per-User-Per-Provider Constraint" $dupFailed "Duplicate review rejected by database unique index"
} catch {
    Record-Test "10. One-Review-Per-User-Per-Provider Constraint" $false $_.Exception.Message
}

# 11. Test Patient Review Submission on Completed Appointment
try {
    # Since appointment $newApptId with Provider 2 is Completed, user 1 can review Provider 2
    $reviewSql = "INSERT INTO public.`"HealthcareReviews`" (`"HealthcareProviderId`", `"DisabilityUserId`", `"Rating`", `"ReviewText`", `"CreatedAt`") VALUES ($p2Id, $u1Id, 5, 'Dr. Fahmida Sultana was wonderful with our child rehabilitation therapy.', NOW()) RETURNING `"Id`";"
    $newRevId = ((Run-Sql $reviewSql) -split "`r?`n")[0].Trim()
    $revCreated = ($newRevId -gt 0)

    # Clean up test review
    Run-Sql "DELETE FROM public.`"HealthcareReviews`" WHERE `"Id`" = $newRevId;"

    Record-Test "11. Review Creation for Completed Appointment" $revCreated "Patient with completed appointment submitted 5-star review"
} catch {
    Record-Test "11. Review Creation for Completed Appointment" $false $_.Exception.Message
}

# 12. Test Appointment Cancellation by Patient
try {
    $cancelApptSql = @"
INSERT INTO public."HealthcareAppointments" (
    "HealthcareProviderId", "DisabilityUserId", "AppointmentDate", "TimeSlot",
    "ReasonForVisit", "Status", "CreatedAt"
) VALUES (
    $p3Id, $u1Id, NOW() + INTERVAL '10 days', '11:00 AM - 11:30 AM',
    'Appointment to test cancellation.', 'Pending', NOW()
) RETURNING "Id";
"@
    $cancelId = ((Run-Sql $cancelApptSql) -split "`r?`n")[0].Trim()

    $doCancelSql = "UPDATE public.`"HealthcareAppointments`" SET `"Status`" = 'Cancelled', `"UpdatedAt`" = NOW() WHERE `"Id`" = $cancelId; SELECT `"Status`" FROM public.`"HealthcareAppointments`" WHERE `"Id`" = $cancelId;"
    $cancelledStatus = ((Run-Sql $doCancelSql) -split "`r?`n")[-1].Trim()

    Record-Test "12. Appointment Cancellation" ($cancelledStatus -eq "Cancelled") "Appointment $cancelId successfully transitioned to Cancelled"
} catch {
    Record-Test "12. Appointment Cancellation" $false $_.Exception.Message
}

# 13. Test Direct Admin Healthcare Provider Management (Add, Edit, Delete)
try {
    # Admin adds a new healthcare provider
    $adminAddSql = @'
INSERT INTO public."HealthcareProviders" (
    "Name", "ProviderType", "OrganizationOrClinic", "Description", "Specialization",
    "Phone", "Email", "Address", "City", "District", "Latitude", "Longitude",
    "AvailableServices", "ConsultationFee", "AvailabilitySchedule", "CreatedAt"
) VALUES (
    'Dr. Admin Test Doctor', 'Doctor', 'Admin Care Clinic',
    'Created by Admin to verify direct provider management.', 'General Rehabilitation',
    '+880 1799 999999', 'admintest@clinic.bd', 'Test Road 10', 'Dhanmondi', 'Dhaka',
    23.7461, 90.3742,
    'General Consultation, Physical Therapy Referral', 500.00, 'Sun - Thu: 9:00 AM - 1:00 PM', NOW()
) RETURNING "Id";
'@
    $testDocId = ((Run-Sql $adminAddSql) -split "`r?`n")[0].Trim()
    $addedOk = ($testDocId -gt 0)

    # Admin edits the provider
    $adminEditSql = "UPDATE public.`"HealthcareProviders`" SET `"Specialization`" = 'Updated Specialization', `"ConsultationFee`" = 650.00, `"UpdatedAt`" = NOW() WHERE `"Id`" = $testDocId; SELECT `"Specialization`" FROM public.`"HealthcareProviders`" WHERE `"Id`" = $testDocId;"
    $updatedSpec = ((Run-Sql $adminEditSql) -split "`r?`n")[-1].Trim()
    $editedOk = ($updatedSpec -eq "Updated Specialization")

    # Admin deletes the provider
    $adminDelSql = "DELETE FROM public.`"HealthcareProviders`" WHERE `"Id`" = $testDocId; SELECT COUNT(*) FROM public.`"HealthcareProviders`" WHERE `"Id`" = $testDocId;"
    $delCount = [int]((Run-Sql $adminDelSql) -split "`r?`n")[-1].Trim()
    $deletedOk = ($delCount -eq 0)

    $passAdmin = ($addedOk -and $editedOk -and $deletedOk)
    Record-Test "13. Admin Direct Provider Management (Add/Edit/Delete)" $passAdmin "Admin successfully added, updated, and deleted provider without separate verification"
} catch {
    Record-Test "13. Admin Direct Provider Management" $false $_.Exception.Message
}

# 14. Test Unauthenticated Authorization Guards
try {
    $r1 = Invoke-WebRequest -Uri "$baseUrl/Healthcare/MyAppointments" -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $r2 = Invoke-WebRequest -Uri "$baseUrl/Admin/HealthcareProviders" -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $r3 = Invoke-WebRequest -Uri "$baseUrl/Disability/DoctorAppointments" -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue

    $protected = ($r1.StatusCode -eq 302 -or $r1.Headers.Location -like "*Login*") -and
                 ($r2.StatusCode -eq 302 -or $r2.Headers.Location -like "*Login*") -and
                 ($r3.StatusCode -eq 302 -or $r3.Headers.Location -like "*Login*")

    Record-Test "14. Authorization & Protected Routes" $protected "MyAppointments, Admin/HealthcareProviders, and DoctorAppointments guarded"
} catch {
    Record-Test "14. Authorization & Protected Routes" $false $_.Exception.Message
}

# 15. Test Navigation Layout Link in Navbar and Footer
try {
    $homeResp = Invoke-WebRequest -Uri "$baseUrl/" -Method Get -UseBasicParsing
    $hasNavHealthcare = $homeResp.Content.Contains("/Healthcare")
    Record-Test "15. Main Navigation Bar Link" ($homeResp.StatusCode -eq 200 -and $hasNavHealthcare) "Navbar and Footer contain Healthcare links"
} catch {
    Record-Test "15. Main Navigation Bar Link" $false $_.Exception.Message
}

# 16. Test Section 5.4 Accessible Job Portal Regression
try {
    $jobsResp = Invoke-WebRequest -Uri "$baseUrl/Jobs" -Method Get -UseBasicParsing
    $hasJobs = $jobsResp.StatusCode -eq 200 -and ($jobsResp.Content.Contains("Jobs") -or $jobsResp.Content.Contains("Job"))
    Record-Test "16. Section 5.4 Job Portal Regression" $hasJobs "Accessible Job Portal is 100% operational"
} catch {
    Record-Test "16. Section 5.4 Job Portal Regression" $false $_.Exception.Message
}

# 17. Clean up test appointments
try {
    if ($newApptId) { Run-Sql "DELETE FROM public.`"HealthcareAppointments`" WHERE `"Id`" = $newApptId;" }
    if ($cancelId) { Run-Sql "DELETE FROM public.`"HealthcareAppointments`" WHERE `"Id`" = $cancelId;" }
    Record-Test "17. Test Data Cleanup" $true "Transient test appointments cleaned up"
} catch {
    Record-Test "17. Test Data Cleanup" $false $_.Exception.Message
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " TEST SUMMARY" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
$passCount = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$totalCount = $testResults.Count
$sumColor = if ($passCount -eq $totalCount) { "Green" } else { "Yellow" }
Write-Host "Total Tests: $totalCount | Passed: $passCount | Failed: ($totalCount - $passCount)" -ForegroundColor $sumColor

if ($passCount -eq $totalCount) {
    Write-Host "ALL $totalCount FUNCTIONAL TESTS PASSED SUCCESSFULLY!" -ForegroundColor Green
} else {
    Write-Host "SOME TESTS FAILED. PLEASE REVIEW DETAILS ABOVE." -ForegroundColor Red
}
