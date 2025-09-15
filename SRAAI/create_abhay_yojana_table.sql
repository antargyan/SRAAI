-- Create AbhayYojanaApplications table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AbhayYojanaApplications' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AbhayYojanaApplications] (
        [OriginalSlumNumber] int NOT NULL,
        [SerialNumber] int NOT NULL,
        [OriginalSlumDwellerName] nvarchar(200) NOT NULL,
        [ApplicantName] nvarchar(200) NOT NULL,
        [VoterListYear] int NULL,
        [VoterListPartNumber] nvarchar(50) NULL,
        [VoterListSerialNumber] int NULL,
        [VoterListBound] nvarchar(100) NULL,
        [SlumUsage] nvarchar(100) NOT NULL,
        [CarpetAreaSqFt] decimal(18,2) NULL,
        [EvidenceDetails] nvarchar(2000) NOT NULL,
        [EligibilityStatus] nvarchar(50) NOT NULL,
        [Remarks] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_AbhayYojanaApplications] PRIMARY KEY ([OriginalSlumNumber])
    );
    
    PRINT 'AbhayYojanaApplications table created successfully';
END
ELSE
BEGIN
    PRINT 'AbhayYojanaApplications table already exists';
END
