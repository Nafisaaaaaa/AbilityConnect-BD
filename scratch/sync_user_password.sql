UPDATE "DisabilityUsers"
SET "PasswordHash" = (SELECT "PasswordHash" FROM "Admins" WHERE "Email" = 'admin@abilityconnect.com')
WHERE "Email" = 'swasthy@gmail.com';
