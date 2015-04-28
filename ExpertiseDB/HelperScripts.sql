-- Resets an evaluated repository, such that a new algorithm comparison may start

START TRANSACTION;

SET @RepositoryToReset = 6;	-- AOSP

UPDATE Repositorys
SET LastUpdate=NULL
WHERE Repositorys.RepositoryId = @RepositoryToReset;

UPDATE Artifacts
SET ModificationCount=0
WHERE Artifacts.RepositoryId = @RepositoryToReset;

UPDATE DeveloperExpertises
SET IsFirstAuthor=FALSE,DeliveriesCount=0
WHERE DeveloperExpertises.DeveloperId IN
(SELECT Developers.DeveloperID
FROM Developers
WHERE Developers.RepositoryId = @RepositoryToReset);


COMMIT;