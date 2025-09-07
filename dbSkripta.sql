create database infoeduka2
go

use infoeduka2
go

-- SQL skripta za Infoeduka (MS SQL Server) ----------------------------------
/* ============================================================
   INFOEDUKA (Custom SQL, no ASP.NET Identity)
   Entities: Group, User, Course, CourseTeacher, CourseStudent,
             FileAsset, Submission, Grade, Notification,
             ImportBatch, ImportRow
   Notes:
     - Roles: 0=Student, 1=Professor, 2=Admin
     - Admin MUST NOT have a Group; Student MUST have a Group;
       Professor may or may not have a Group (adjustable below).
   ============================================================ */

SET XACT_ABORT ON;
BEGIN TRAN;

---------------------------------------------------------------
-- 1) Core lookup: Group
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.[Group]', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.[Group](
      Id   INT IDENTITY(1,1) PRIMARY KEY,
      Name NVARCHAR(100) NOT NULL UNIQUE
  );
END;

---------------------------------------------------------------
-- 2) User (custom auth)
--    Role: 0=Student, 1=Professor, 2=Admin
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.[User](
      Id            INT IDENTITY(1,1) PRIMARY KEY,
      FirstName     NVARCHAR(50)  NOT NULL,
      LastName      NVARCHAR(50)  NOT NULL,
      Email         NVARCHAR(256) NOT NULL UNIQUE,
      PasswordHash  NVARCHAR(500) NOT NULL,
      Role          TINYINT NOT NULL
                     CONSTRAINT CK_User_Role
                     CHECK (Role IN (0,1,2)),
      GroupId       INT NULL
                     CONSTRAINT FK_User_Group
                     FOREIGN KEY REFERENCES dbo.[Group](Id),
      IsFirstLogin  BIT NOT NULL
                     CONSTRAINT DF_User_IsFirstLogin DEFAULT (1)
  );

  -- Make the Admin/Student/Professor ↔ Group rule explicit:
  ALTER TABLE dbo.[User] WITH CHECK
  ADD CONSTRAINT CK_User_Role_Group
  CHECK (
       (Role = 2 AND GroupId IS NULL)     -- Admin => must NOT have a group
    OR (Role = 0 AND GroupId IS NOT NULL) -- Student => MUST have a group
    OR (Role = 1)                         -- Professor => optional
  );

  CREATE INDEX IX_User_GroupId ON dbo.[User](GroupId);
END;

---------------------------------------------------------------
-- 3) Course (Kolegij)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.Course', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Course(
      Id               INT IDENTITY(1,1) PRIMARY KEY,
      Name             NVARCHAR(200) NOT NULL,
      ShortDescription NVARCHAR(500) NULL,
      CONSTRAINT UQ_Course_Name UNIQUE (Name)
  );
END;

---------------------------------------------------------------
-- 4) CourseTeacher (Professor/Assistant assignment)
--    NOTE: cannot hard-enforce Role=Professor at DB level without triggers;
--          enforce in app/service layer. We still ensure uniqueness.
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.CourseTeacher', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CourseTeacher(
      Id         INT IDENTITY(1,1) PRIMARY KEY,
      CourseId   INT NOT NULL
                  CONSTRAINT FK_CourseTeacher_Course
                  FOREIGN KEY REFERENCES dbo.Course(Id),
      TeacherId  INT NOT NULL
                  CONSTRAINT FK_CourseTeacher_User
                  FOREIGN KEY REFERENCES dbo.[User](Id),
      IsAssistant BIT NOT NULL CONSTRAINT DF_CourseTeacher_IsAssistant DEFAULT (0),
      CONSTRAINT UQ_CourseTeacher UNIQUE (CourseId, TeacherId)
  );

  CREATE INDEX IX_CourseTeacher_CourseId ON dbo.CourseTeacher(CourseId);
  CREATE INDEX IX_CourseTeacher_TeacherId ON dbo.CourseTeacher(TeacherId);
END;

---------------------------------------------------------------
-- 5) CourseStudent (enrollment)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.CourseStudent', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CourseStudent(
      Id        INT IDENTITY(1,1) PRIMARY KEY,
      CourseId  INT NOT NULL
                 CONSTRAINT FK_CourseStudent_Course
                 FOREIGN KEY REFERENCES dbo.Course(Id),
      StudentId INT NOT NULL
                 CONSTRAINT FK_CourseStudent_User
                 FOREIGN KEY REFERENCES dbo.[User](Id),
      CONSTRAINT UQ_CourseStudent UNIQUE (CourseId, StudentId)
  );

  CREATE INDEX IX_CourseStudent_CourseId ON dbo.CourseStudent(CourseId);
  CREATE INDEX IX_CourseStudent_StudentId ON dbo.CourseStudent(StudentId);
END;

---------------------------------------------------------------
-- 6) FileAsset (uploaded files: materials or submissions)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.FileAsset', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.FileAsset(
      Id           INT IDENTITY(1,1) PRIMARY KEY,
      FileName     NVARCHAR(260)  NOT NULL,
      StoredPath   NVARCHAR(400)  NOT NULL, -- e.g. /uploads/2025/08/abc.pdf
      ContentType  NVARCHAR(100)  NOT NULL,
      SizeBytes    BIGINT         NOT NULL,
      CourseId     INT            NOT NULL
                    CONSTRAINT FK_FileAsset_Course
                    FOREIGN KEY REFERENCES dbo.Course(Id),
      UploadedById INT            NOT NULL
                    CONSTRAINT FK_FileAsset_Uploader
                    FOREIGN KEY REFERENCES dbo.[User](Id),
      UploadedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_FileAsset_UploadedAt DEFAULT (SYSUTCDATETIME())
  );

  CREATE INDEX IX_FileAsset_CourseId     ON dbo.FileAsset(CourseId);
  CREATE INDEX IX_FileAsset_UploadedById ON dbo.FileAsset(UploadedById);
  CREATE INDEX IX_FileAsset_UploadedAt   ON dbo.FileAsset(UploadedAt);
END;

---------------------------------------------------------------
-- 7) Submission (student seminar mapped to a file)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.Submission', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Submission(
      Id           INT IDENTITY(1,1) PRIMARY KEY,
      FileAssetId  INT NOT NULL
                    CONSTRAINT FK_Submission_FileAsset
                    FOREIGN KEY REFERENCES dbo.FileAsset(Id),
      CourseId     INT NOT NULL
                    CONSTRAINT FK_Submission_Course
                    FOREIGN KEY REFERENCES dbo.Course(Id),
      StudentId    INT NOT NULL
                    CONSTRAINT FK_Submission_User
                    FOREIGN KEY REFERENCES dbo.[User](Id),
      Reviewed     BIT NOT NULL CONSTRAINT DF_Submission_Reviewed DEFAULT (0)
  );

  -- A student shouldn't submit the SAME file twice for the SAME course:
  ALTER TABLE dbo.Submission
    ADD CONSTRAINT UQ_Submission UNIQUE (CourseId, StudentId, FileAssetId);

  CREATE INDEX IX_Submission_CourseId  ON dbo.Submission(CourseId);
  CREATE INDEX IX_Submission_StudentId ON dbo.Submission(StudentId);
END;

---------------------------------------------------------------
-- 8) Grade (points for a submission)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.Grade', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Grade(
      Id            INT IDENTITY(1,1) PRIMARY KEY,
      SubmissionId  INT NOT NULL
                     CONSTRAINT FK_Grade_Submission
                     FOREIGN KEY REFERENCES dbo.Submission(Id),
      TeacherId     INT NOT NULL
                     CONSTRAINT FK_Grade_Teacher
                     FOREIGN KEY REFERENCES dbo.[User](Id),
      Points        DECIMAL(5,2) NOT NULL
                     CONSTRAINT CK_Grade_Points CHECK (Points >= 0),
      GradedAt      DATETIME2(3) NOT NULL CONSTRAINT DF_Grade_GradedAt DEFAULT (SYSUTCDATETIME())
  );

  -- One grade per submission:
  CREATE UNIQUE INDEX UX_Grade_SubmissionId ON dbo.Grade(SubmissionId);

  CREATE INDEX IX_Grade_TeacherId ON dbo.Grade(TeacherId);
END;

---------------------------------------------------------------
-- 9) Notification (in-app notifications)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.Notification', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Notification(
      Id         INT IDENTITY(1,1) PRIMARY KEY,
      ToUserId   INT NOT NULL
                  CONSTRAINT FK_Notification_ToUser
                  FOREIGN KEY REFERENCES dbo.[User](Id),
      FromUserId INT NULL
                  CONSTRAINT FK_Notification_FromUser
                  FOREIGN KEY REFERENCES dbo.[User](Id),
      Title      NVARCHAR(200)  NOT NULL,
      Body       NVARCHAR(1000) NULL,
      Link       NVARCHAR(400)  NULL,
      CreatedAt  DATETIME2(3)   NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT (SYSUTCDATETIME()),
      IsRead     BIT            NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT (0)
  );

  CREATE INDEX IX_Notification_ToUserId   ON dbo.Notification(ToUserId, IsRead, CreatedAt);
  CREATE INDEX IX_Notification_CreatedAt  ON dbo.Notification(CreatedAt);
END;

---------------------------------------------------------------
-- 10) Import logs (.xls user import)
---------------------------------------------------------------
IF OBJECT_ID(N'dbo.ImportBatch', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ImportBatch(
      Id             INT IDENTITY(1,1) PRIMARY KEY,
      CreatedAt      DATETIME2(3) NOT NULL CONSTRAINT DF_ImportBatch_CreatedAt DEFAULT (SYSUTCDATETIME()),
      CreatedById    INT NULL
                      CONSTRAINT FK_ImportBatch_User
                      FOREIGN KEY REFERENCES dbo.[User](Id),
      SourceFileName NVARCHAR(260) NOT NULL
  );

  CREATE INDEX IX_ImportBatch_CreatedAt ON dbo.ImportBatch(CreatedAt);
END;

IF OBJECT_ID(N'dbo.ImportRow', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.ImportRow(
      Id            INT IDENTITY(1,1) PRIMARY KEY,
      ImportBatchId INT NOT NULL
                     CONSTRAINT FK_ImportRow_Batch
                     FOREIGN KEY REFERENCES dbo.ImportBatch(Id),
      RowNumber     INT NOT NULL,
      Data          NVARCHAR(MAX) NOT NULL, -- JSON snapshot of the row
      IsSuccess     BIT NOT NULL CONSTRAINT DF_ImportRow_IsSuccess DEFAULT (0),
      Error         NVARCHAR(1000) NULL
  );

  -- Validate JSON if you use OPENJSON/etc.
  ALTER TABLE dbo.ImportRow WITH CHECK
    ADD CONSTRAINT CK_ImportRow_Data_IsJson
    CHECK (ISJSON(Data) = 1);

  CREATE INDEX IX_ImportRow_ImportBatchId ON dbo.ImportRow(ImportBatchId);
END;

COMMIT TRAN;
GO

/* ============================================================
   OPTIONAL VARIANTS for Group rule:
   - If professors should ALSO be required to have a group, then:
     ALTER TABLE dbo.[User] DROP CONSTRAINT CK_User_Role_Group;
     ALTER TABLE dbo.[User] WITH CHECK
     ADD CONSTRAINT CK_User_Role_Group
     CHECK (
          (Role = 2 AND GroupId IS NULL)
       OR (Role IN (0,1) AND GroupId IS NOT NULL)
     );
   ============================================================ */

---------------------------------------------------------------
-- PROCEDURA ZA uploadanje user-a (bulk) za ADMINA (pars ide preko C#)
---------------------------------------------------------------

/* 1) TVP for incoming rows (from .xls after parsing in the app) */
IF TYPE_ID(N'dbo.udt_ImportedUser') IS NULL
CREATE TYPE dbo.udt_ImportedUser AS TABLE
(
    FirstName NVARCHAR(50)  NOT NULL,
    LastName  NVARCHAR(50)  NOT NULL,
    RoleName  NVARCHAR(20)  NOT NULL,   -- 'student' | 'nastavnik' | 'professor' | 'admin'
    Email     NVARCHAR(256) NOT NULL,
    GroupId   INT           NULL,       -- required for students, NULL for admins
    CourseId  INT           NULL        -- optional: target course (if provided)
);
GO

IF OBJECT_ID(N'dbo.sp_BulkUpsertUsers', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_BulkUpsertUsers;
GO

CREATE OR ALTER PROCEDURE dbo.sp_BulkUpsertUsers
(
    @Rows                dbo.udt_ImportedUser READONLY,
    @CreatedById         INT              = NULL,
    @DefaultPasswordHash NVARCHAR(500)    = N'',
    @BatchId             INT              OUTPUT,
    @SourceFileName      NVARCHAR(260)    = N'upload.xls'
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    /* 1) ImportBatch log */
    INSERT INTO dbo.ImportBatch (CreatedById, SourceFileName)
    VALUES (@CreatedById, @SourceFileName);
    SET @BatchId = SCOPE_IDENTITY();

    /* 2) Normalize roles and stage rows */
    ;WITH Norm AS
    (
        SELECT
            ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS RowNumber,
            FirstName, LastName, Email, GroupId, CourseId,
            LOWER(RoleName) AS RoleNameLower,
            CASE
                WHEN LOWER(RoleName) IN (N'student', N'studenti') THEN 0
                WHEN LOWER(RoleName) IN (N'nastavnik', N'professor', N'profesor') THEN 1
                WHEN LOWER(RoleName) = N'admin' THEN 2
                ELSE NULL
            END AS RoleCode
        FROM @Rows
    )
    SELECT *
    INTO #Rows
    FROM Norm;

    /* 3) Log raw rows to ImportRow as JSON */
    INSERT INTO dbo.ImportRow (ImportBatchId, RowNumber, Data, IsSuccess, Error)
    SELECT
        @BatchId,
        r.RowNumber,
        CONCAT(
            N'{"FirstName":"', STRING_ESCAPE(r.FirstName, 'json'),
            N'","LastName":"',  STRING_ESCAPE(r.LastName,  'json'),
            N'","RoleName":"',  STRING_ESCAPE(r.RoleNameLower, 'json'),
            N'","Email":"',     STRING_ESCAPE(r.Email,     'json'),
            N'","GroupId":',    CASE WHEN r.GroupId IS NULL THEN N'null' ELSE CONVERT(NVARCHAR(20), r.GroupId) END,
            N',"CourseId":',    CASE WHEN r.CourseId IS NULL THEN N'null' ELSE CONVERT(NVARCHAR(20), r.CourseId) END,
            N'}'
        ),
        0,
        NULL
    FROM #Rows r;

    /* 4) Pre-validate and collect errors */
    SELECT r.RowNumber, Err =
           CASE
                WHEN r.RoleCode IS NULL THEN N'Invalid role'
                WHEN r.RoleCode = 0 AND r.GroupId IS NULL THEN N'Student must have GroupId'
                WHEN r.RoleCode = 2 AND r.GroupId IS NOT NULL THEN N'Admin must not have GroupId'
                WHEN r.GroupId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.[Group] g WHERE g.Id = r.GroupId) THEN N'GroupId not found'
                WHEN r.CourseId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Course c WHERE c.Id = r.CourseId) THEN N'CourseId not found'
                ELSE NULL
           END
    INTO #Errors
    FROM #Rows r;

    -- Mark erroneous rows in ImportRow
    UPDATE ir
    SET IsSuccess = 0,
        Error = e.Err
    FROM dbo.ImportRow ir
    JOIN #Errors e ON e.RowNumber = ir.RowNumber
    WHERE ir.ImportBatchId = @BatchId
      AND e.Err IS NOT NULL;

    /* 5) Keep only valid rows */
    SELECT r.*
    INTO #Valid
    FROM #Rows r
    LEFT JOIN #Errors e ON e.RowNumber = r.RowNumber
    WHERE e.Err IS NULL;

    /* 6) Prepare temp table for MERGE output */
    CREATE TABLE #Upserted(
      MergeAction NVARCHAR(10),
      InsertedUserId INT,
      Email NVARCHAR(256)
    );

    /* 7) Upsert users by Email */
    MERGE dbo.[User] WITH (HOLDLOCK) AS T
    USING (
        SELECT FirstName, LastName, Email, GroupId, RoleCode
        FROM #Valid
    ) AS S
      ON T.Email = S.Email
    WHEN MATCHED THEN
        UPDATE SET
            T.FirstName = S.FirstName,
            T.LastName  = S.LastName,
            T.Role      = S.RoleCode,
            T.GroupId   = S.GroupId
    WHEN NOT MATCHED THEN
        INSERT (FirstName, LastName, Email, PasswordHash, Role, GroupId, IsFirstLogin)
        VALUES (S.FirstName, S.LastName, S.Email, @DefaultPasswordHash, S.RoleCode, S.GroupId, 1)
    OUTPUT $action AS MergeAction, inserted.Id AS InsertedUserId, inserted.Email INTO #Upserted;

    /* 8) Auto-enroll if CourseId provided */
    -- Students
    INSERT INTO dbo.CourseStudent (CourseId, StudentId)
    SELECT DISTINCT v.CourseId, u.Id
    FROM #Valid v
    JOIN dbo.[User] u ON u.Email = v.Email
    WHERE v.CourseId IS NOT NULL
      AND v.RoleCode = 0
      AND NOT EXISTS (
           SELECT 1 FROM dbo.CourseStudent cs WHERE cs.CourseId = v.CourseId AND cs.StudentId = u.Id
      );

    -- Professors
    INSERT INTO dbo.CourseTeacher (CourseId, TeacherId, IsAssistant)
    SELECT DISTINCT v.CourseId, u.Id, 0
    FROM #Valid v
    JOIN dbo.[User] u ON u.Email = v.Email
    WHERE v.CourseId IS NOT NULL
      AND v.RoleCode = 1
      AND NOT EXISTS (
           SELECT 1 FROM dbo.CourseTeacher ct WHERE ct.CourseId = v.CourseId AND ct.TeacherId = u.Id
      );

    /* 9) Mark valid rows as success */
    UPDATE ir
    SET IsSuccess = 1,
        Error = NULL
    FROM dbo.ImportRow ir
    WHERE ir.ImportBatchId = @BatchId
      AND NOT EXISTS (SELECT 1 FROM #Errors e WHERE e.RowNumber = ir.RowNumber  AND e.Err IS NOT NULL );

    COMMIT TRAN;
END
GO

/* ============================================================
   OPTIONAL: cascade cleanup on enrollments/assignments
   Uncomment if you want automatic cleanup on deletes.
   ------------------------------------------------------------
-- CourseStudent cascades
--ALTER TABLE dbo.CourseStudent
--  DROP CONSTRAINT FK_CourseStudent_Course, FK_CourseStudent_User;
--ALTER TABLE dbo.CourseStudent
--  ADD CONSTRAINT FK_CourseStudent_Course FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id) ON DELETE CASCADE,
--      CONSTRAINT FK_CourseStudent_User   FOREIGN KEY (StudentId) REFERENCES dbo.[User](Id) ON DELETE CASCADE;

-- CourseTeacher cascades
--ALTER TABLE dbo.CourseTeacher
--  DROP CONSTRAINT FK_CourseTeacher_Course, FK_CourseTeacher_User;
--ALTER TABLE dbo.CourseTeacher
--  ADD CONSTRAINT FK_CourseTeacher_Course FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id) ON DELETE CASCADE,
--      CONSTRAINT FK_CourseTeacher_User   FOREIGN KEY (TeacherId) REFERENCES dbo.[User](Id) ON DELETE CASCADE;
============================================================ */

-------------------TEST :::: UBACIVANJE NEKIH PODATAKA ::::::::____________________-

-- Smoke test: one student and one professor, with a course and group
INSERT INTO dbo.[Group](Name) VALUES (N'GR1');
INSERT INTO dbo.Course(Name, ShortDescription) VALUES (N'Math 101', N'Basics');
DECLARE @CourseId INT = SCOPE_IDENTITY(); -- last insert was Course
-- if unsure, just SELECT @CourseId = MIN(Id) FROM dbo.Course;

DECLARE @T dbo.udt_ImportedUser;
INSERT INTO @T (FirstName, LastName, RoleName, Email, GroupId, CourseId)
VALUES
  (N'Ivan', N'Ivić', N'student',   N'ivan@example.com', 1, @CourseId),
  (N'Ana',  N'Nast.', N'nastavnik', N'ana@example.com',  NULL, @CourseId);

DECLARE @BatchId INT;
EXEC dbo.sp_BulkUpsertUsers
  @Rows = @T,
  @CreatedById = NULL,
  @DefaultPasswordHash = N'<<put-valid-hash-here>>',
  @BatchId = @BatchId OUTPUT,
  @SourceFileName = N'test.xls';

SELECT @BatchId AS BatchId;
SELECT * FROM dbo.[User];
SELECT * FROM dbo.CourseStudent;
SELECT * FROM dbo.CourseTeacher;
SELECT * FROM dbo.ImportRow WHERE ImportBatchId = @BatchId;
