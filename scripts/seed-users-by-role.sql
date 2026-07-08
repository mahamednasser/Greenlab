/*
  Seed one user for each role in Greenlab.Users.
  Roles:
    0 = Admin
    1 = Teacher
    2 = TeacherAssistant

  IMPORTANT:
  - PasswordHash must be an ASP.NET Identity hash (PasswordHasher<User>),
    not SHA2 and not plain text.
  - Replace the placeholder hashes below before running.
*/

SET NOCOUNT ON;

DECLARE @AdminEmail NVARCHAR(256) = N'admin@greenlab.local';
DECLARE @TeacherEmail NVARCHAR(256) = N'teacher@greenlab.local';
DECLARE @AssistantEmail NVARCHAR(256) = N'assistant@greenlab.local';

DECLARE @AdminHash NVARCHAR(MAX) = N'AQAAAAEAACcQAAAAEILE0h1nOylZ41qYBAXDgMW0BuM2TaaJaqn2QgZMt0vZ5PLyL2ZkcI0JbiTKnfn3UA==';
DECLARE @TeacherHash NVARCHAR(MAX) = N'AQAAAAEAACcQAAAAEEJImOGqNFZt1fl9OpA6UpU+JYNF4DWWLyqYRh+dfCG7b0tUWQFGW/JAWHC+o7ZoMw==';
DECLARE @AssistantHash NVARCHAR(MAX) = N'AQAAAAEAACcQAAAAENNbzZRL0AGnR/nmz9evXiB6gvDf94j/AQxrSlsXZLQvPVwRibjzHCBE4kZDtoQf6w==';

IF @AdminHash LIKE N'REPLACE_WITH_%'
   OR @TeacherHash LIKE N'REPLACE_WITH_%'
   OR @AssistantHash LIKE N'REPLACE_WITH_%'
BEGIN
    THROW 50001, 'Replace password hash placeholders before executing script.', 1;
END

;WITH SeedUsers AS
(
    SELECT
        @AdminEmail AS Email,
        N'Admin User' AS FullName,
        @AdminHash AS PasswordHash,
        CAST(0 AS INT) AS Role
    UNION ALL
    SELECT
        @TeacherEmail,
        N'Teacher User',
        @TeacherHash,
        CAST(1 AS INT)
    UNION ALL
    SELECT
        @AssistantEmail,
        N'TA User',
        @AssistantHash,
        CAST(2 AS INT)
)
MERGE INTO Users AS target
USING SeedUsers AS src
    ON target.Email = src.Email
WHEN MATCHED THEN
    UPDATE SET
        target.FullName = src.FullName,
        target.PasswordHash = src.PasswordHash,
        target.Role = src.Role,
        target.IsActive = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Email, FullName, PasswordHash, Role, CreatedAt, IsActive)
    VALUES (src.Email, src.FullName, src.PasswordHash, src.Role, SYSUTCDATETIME(), 1);

SELECT Id, Email, FullName, Role, IsActive, CreatedAt
FROM Users
WHERE Email IN (@AdminEmail, @TeacherEmail, @AssistantEmail)
ORDER BY Role;
