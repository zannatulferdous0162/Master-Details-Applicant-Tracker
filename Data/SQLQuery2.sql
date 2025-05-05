	CREATE TYPE dbo.ExperienceType AS TABLE
(
	CompanyName varchar(255),
	Designation varchar(255),
	YearsWorked int
)
GO

-- Create Applicant with Experience
CREATE PROCEDURE sp_CreateApplicantWithExperience
	@Date datetime2,
	@Name varchar(255),
	@Gender varchar(50),
	@DOB date,
	@Age int,
	@Qualification varchar(255),
	@TotalExperience int,
	@PhotoUrl varchar(255),
	@ExperienceType dbo.ExperienceType READONLY -- Table-Valued Parameter
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO Applicant ([Date], [Name], [Gender], [DOB], [Age], [Qualification], [TotalExperience], [PhotoUrl])
	VALUES (@Date, @Name, @Gender, @DOB, @Age, @Qualification, @TotalExperience, @PhotoUrl)

	DECLARE @ApplicantId INT = SCOPE_IDENTITY()

	INSERT INTO Experience (ApplicantId, CompanyName, Designation, YearsWorked)
	SELECT @ApplicantId, CompanyName, Designation, YearsWorked FROM @ExperienceType
END
GO

-- Update Applicant with Experience (delete and re-insert experience)
CREATE PROCEDURE sp_UpdateApplicantWithExperience
	@Id INT,
	@Date datetime2,
	@Name varchar(255),
	@Gender varchar(50),
	@DOB date,
	@Age int,
	@Qualification varchar(255),
	@TotalExperience int,
	@PhotoUrl varchar(255),
	@ExperienceType dbo.ExperienceType READONLY
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE Applicant
	SET [Date] = @Date, [Name] = @Name, [Gender] = @Gender,
		[DOB] = @DOB, [Age] = @Age, [Qualification] = @Qualification,
		[TotalExperience] = @TotalExperience, [PhotoUrl] = @PhotoUrl
	WHERE Id = @Id

	DELETE FROM Experience WHERE ApplicantId = @Id

	INSERT INTO Experience (ApplicantId, CompanyName, Designation, YearsWorked)
	SELECT @Id, CompanyName, Designation, YearsWorked FROM @ExperienceType
END
GO

-- Delete Applicant and Experiences
CREATE PROCEDURE sp_DeleteApplicant
	@Id INT
AS
BEGIN
	DELETE FROM Experience WHERE ApplicantId = @Id
	DELETE FROM Applicant WHERE Id = @Id
END
GO

-- Get All Applicants with their Experiences (optional to join)
CREATE PROCEDURE sp_GetAllApplicants
AS
BEGIN
	SELECT * FROM Applicant
END
GO

-- Get Single Applicant by ID
create PROCEDURE sp_GetApplicantById
	@Id INT
AS
BEGIN
	SELECT * FROM Applicant WHERE Id = @Id
	SELECT * FROM Experience WHERE ApplicantId = @Id
END
GO


CREATE PROCEDURE sp_GetExperiencesByApplicantId
    @ApplicantId INT
AS
BEGIN
    SELECT * FROM Experience WHERE ApplicantId = @ApplicantId
END
GO
