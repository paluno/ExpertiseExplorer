-- -----------------------------------------------------
-- Data for table `expertisedb`.`ArtifactTypes`
-- -----------------------------------------------------
START TRANSACTION;
USE `expertisedb`;
INSERT INTO `expertisedb`.`ArtifactTypes` (`ArtifactTypeId`, `Name`) VALUES (1, 'File');

COMMIT;

-- -----------------------------------------------------
-- stored procedure GetDeveloperExpertiseSum(int)
-- -----------------------------------------------------
START TRANSACTION;
USE `expertisedb`;

CREATE PROCEDURE `GetDeveloperExpertiseSum`(IN repoId INT)
SELECT DeveloperExpertises.DeveloperId, RepositoryId, sum(DeveloperExpertiseValues.Value) AS ExSum 
FROM DeveloperExpertises
JOIN Developers
ON DeveloperExpertises.DeveloperId = Developers.DeveloperId
JOIN DeveloperExpertiseValues
ON DeveloperExpertises.DeveloperExpertiseId = DeveloperExpertiseValues.DeveloperExpertiseId
WHERE RepositoryId = repoId
GROUP BY DeveloperId ORDER BY ExSum DESC;

COMMIT;

-- -----------------------------------------------------
-- stored procedure GetDevelopersForPath(varchar(255))
-- -----------------------------------------------------

START TRANSACTION;
USE `expertisedb`;

CREATE PROCEDURE `GetDevelopersForPath`(IN path VARCHAR(255))
SELECT DeveloperId, sum(IsFirstAuthor) as IsFirstAuthorCount, sum(DeliveriesCount) as DeliveriesCount FROM DeveloperExpertises
JOIN Artifacts
ON Artifacts.artifactId = DeveloperExpertises.artifactId
WHERE Artifacts.Name LIKE path
GROUP BY DeveloperId;

COMMIT;

-- -----------------------------------------------------
-- stored procedure GetDevelopersWOPath()
-- -----------------------------------------------------

START TRANSACTION;
USE `expertisedb`;

CREATE PROCEDURE `GetDevelopersWOPath`()
SELECT DeveloperId, sum(IsFirstAuthor) as IsFirstAuthorCount, sum(DeliveriesCount) as DeliveriesCount FROM DeveloperExpertises
JOIN Artifacts
ON Artifacts.artifactId = DeveloperExpertises.artifactId
WHERE Artifacts.Name NOT LIKE '%/%'
GROUP BY DeveloperId;

COMMIT;

-- -----------------------------------------------------
-- stored procedure GetUserForLastRevisionOfBefore(int, datetime)
-- -----------------------------------------------------

START TRANSACTION;
USE `expertisedb`;

CREATE PROCEDURE `GetUserForLastRevisionOfBefore`(IN filenameid int, IN beforeDatetime DATETIME)
SELECT User FROM FileRevisions
JOIN Revisions
ON Revisions.revisionid = FileRevisions.revisionid
WHERE FileRevisions.filenameid = filenameid and time < beforeDatetime
ORDER BY time DESC
LIMIT 1;

COMMIT;

-- -----------------------------------------------------
-- stored procedure GetUsersOfRevisionsOfBefore(int, datetime)
-- -----------------------------------------------------


START TRANSACTION;
USE `expertisedb`;

CREATE PROCEDURE `GetUsersOfRevisionsOfBefore`(IN filenameid int, IN beforeDatetime DATETIME)
SELECT User FROM FileRevisions
JOIN Revisions
ON Revisions.revisionid = FileRevisions.revisionid
WHERE FileRevisions.filenameid = filenameid and time < beforeDatetime
ORDER BY time DESC;

COMMIT;


-- -----------------------------------------------------
-- stored procedure GetActualReviewersGrouped()
-- -----------------------------------------------------


START TRANSACTION;
USE `expertisedb`;

CREATE PROCEDURE `GetActualReviewersGrouped`()
SELECT ChangeId, count(changeid) as Count-- Shouldn't it count on artifactid?
FROM ActualReviewers
GROUP BY ChangeId;

COMMIT;

-- -----------------------------------------------------
-- stored procedure StoreDeveloperExpertiseValue()
-- -----------------------------------------------------

DELIMITER $$

CREATE PROCEDURE StoreDeveloperExpertiseValue (IN DeveloperName VARCHAR(255), IN ExpertiseValue double, IN ArtId INT, IN RepId INT, IN AlgId INT)
BEGIN
DECLARE developerId int;
DECLARE DevExpId int;

SET developerId = (SELECT Developers.DeveloperId FROM Developers WHERE Developers.name=DeveloperName AND Developers.RepositoryId=RepId);
IF developerId IS NULL THEN
	INSERT INTO Developers(name,RepositoryId) VALUES (DeveloperName,RepId);
	SET developerId = LAST_INSERT_ID();
END IF;

SET DevExpId = (SELECT d.DeveloperExpertiseId FROM DeveloperExpertises d where d.DeveloperId = developerId and d.ArtifactId = ArtId);
IF DevExpId IS NULL THEN
	INSERT INTO DeveloperExpertises(DeveloperId,ArtifactId,DeliveriesCount,IsFirstAuthor,Inferred) values (developerId,ArtId,0,false,true);
	SET DevExpId = LAST_INSERT_ID();
END IF;

REPLACE INTO DeveloperExpertiseValues(Value,DeveloperExpertiseId,AlgorithmId) VALUES (ExpertiseValue,DevExpId,AlgId);

END$$

-- -----------------------------------------------------
-- constraint for DeveloperExpertiseValues
-- -----------------------------------------------------

ALTER TABLE DeveloperExpertiseValues
ADD CONSTRAINT uc_identity UNIQUE (DeveloperExpertiseId,AlgorithmId);

-- -----------------------------------------------------
-- Index for faster LIKE searches on artifact names (e.g. for FPS algorithm)
-- -----------------------------------------------------
ALTER TABLE Artifacts
ADD INDEX IX_ArtifactName USING BTREE (Name ASC);


-- -----------------------------------------------------
-- Index for faster VCS log inserts
-- -----------------------------------------------------
ALTER TABLE FileNames
ADD INDEX IX_FileNamesName USING BTREE (Name ASC);
