SELECT 'Volunteers:' as tbl;
SELECT "Id", "FullName", "Email", "City", "IsAvailable", "IsVerified" FROM "Volunteers";

SELECT 'DisabilityUsers:' as tbl;
SELECT "Id", "FullName", "Email" FROM "DisabilityUsers";

SELECT 'Admins:' as tbl;
SELECT "Id", "FullName", "Email" FROM "Admins";

SELECT 'Organizations:' as tbl;
SELECT "Id", "OrganizationName", "Email", "IsVerified" FROM "Organizations";
