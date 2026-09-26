SELECT 'Volunteer' as role, "Id", "Email", "FullName", "City", "IsVerified", "IsAvailable" FROM "Volunteers";
SELECT 'Org' as role, "Id", "Email", "OrganizationName" FROM "Organizations";
SELECT "Id", "Title", "OrganizationId", "JobStatus" FROM "Jobs" LIMIT 5;
