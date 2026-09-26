-- Sync test passwords to Admin@123
UPDATE "DisabilityUsers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com');

UPDATE "Volunteers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsAvailable" = true,
    "IsVerified" = true,
    "City" = 'Dhaka',
    "District" = 'Dhaka',
    "AssistanceTypes" = 'Hospital Visits, Shopping Assistance, Travel Assistance, Document Submission, Daily Support'
WHERE "Email" = 'halima@gmail.com';

UPDATE "Volunteers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com'),
    "IsAvailable" = true,
    "IsVerified" = true,
    "City" = 'Chittagong',
    "District" = 'Chittagong',
    "AssistanceTypes" = 'Hospital Visits, Shopping Assistance, Travel Assistance, Document Submission, Daily Support'
WHERE "Email" = 'nafu@gmail.com';

UPDATE "Organizations"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com');
