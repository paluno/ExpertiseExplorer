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


/*
Earlier versions of AlgorithmRunner created multiple ActualReviewers entries for one actual review.
This script integrates these duplicate entries. It has to be run multiple times to find all duplicates.
*/

/* Move Reviewer Computations with duplicate ActualReviewers to the oldest ActualReviewer */
UPDATE ComputedReviewers
INNER JOIN
(SELECT MIN(ActualReviewerId) AS EarliestReviewerId,MAX(ActualReviewerID) AS LatestReviewerId
FROM ActualReviewers
GROUP BY ArtifactId,ChangeId,ActivityId
HAVING EarliestReviewerId<>LatestReviewerId) DuplicateReviewPairs ON ComputedReviewers.ActualReviewerId = DuplicateReviewPairs.LatestReviewerId
SET ActualReviewerId = DuplicateReviewPairs.EarliestReviewerId;

/* Delete all ActualReviewers entries with no corresponding ComputedReviewers
   (they became orphans in the first update) */
DELETE FROM ActualReviewers
WHERE ActualReviewers.ActualReviewerId NOT IN
(SELECT DISTINCT ComputedReviewers.ActualReviewerId
FROM ComputedReviewers);

/* If part of AlgorithmRunner was executed twice, the ComputedReviewer entries
   that were first assigned to different, but identical ActualReviewers
   might match each other and therefore one of them can be deleted */

DELETE OriginalReviewers
FROM ComputedReviewers OriginalReviewers
INNER JOIN ComputedReviewers Doppelgangers ON 
	OriginalReviewers.ActualReviewerId=Doppelgangers.ActualReviewerId AND
	OriginalReviewers.AlgorithmId=Doppelgangers.AlgorithmId AND
	OriginalReviewers.ComputedReviewerId<Doppelgangers.ComputedReviewerId
WHERE
	OriginalReviewers.Expert1=Doppelgangers.Expert1 AND
	OriginalReviewers.Expert1Value=Doppelgangers.Expert1Value AND

	OriginalReviewers.Expert2=Doppelgangers.Expert2 AND
	OriginalReviewers.Expert2Value=Doppelgangers.Expert2Value AND

	OriginalReviewers.Expert3=Doppelgangers.Expert3 AND
	OriginalReviewers.Expert3Value=Doppelgangers.Expert3Value AND

	OriginalReviewers.Expert4=Doppelgangers.Expert4 AND
	OriginalReviewers.Expert4Value=Doppelgangers.Expert4Value AND

	OriginalReviewers.Expert5=Doppelgangers.Expert5 AND
	OriginalReviewers.Expert5Value=Doppelgangers.Expert5Value;


/* The ComputedReviewers might also be different, though. For example,
   if a debug run first produced erroneous results and a patch
   corrected the fault, so the later results are not erroneous.
   In this case, the earlier results have to be deleted. */

DELETE OriginalReviewers
FROM ComputedReviewers OriginalReviewers
INNER JOIN ComputedReviewers Doppelgangers ON 
	OriginalReviewers.ActualReviewerId=Doppelgangers.ActualReviewerId AND
	OriginalReviewers.ComputedReviewerId<Doppelgangers.ComputedReviewerId AND
	OriginalReviewers.AlgorithmId=Doppelgangers.AlgorithmId;


/* ActualReviewers shall have an additional RepositoryId column.
   This speed up statistics for individual repositories. */

ALTER TABLE `ActualReviewers` 
ADD COLUMN `RepositoryId` INT NULL AFTER `ArtifactId`,
ADD INDEX `FK_RepositoryActualReviewer_idx` (`RepositoryId` ASC);
ALTER TABLE `ActualReviewers` 
ADD CONSTRAINT `FK_RepositoryActualReviewer`
  FOREIGN KEY (`RepositoryId`)
  REFERENCES `Repositorys` (`RepositoryId`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;


/* Now the new column RepositoryId contains all NULLs. Time to update AlgorithmRunner to write the RepositoryId. 
   Afterwards: Add RepositoryIds to the old NULL entries */

UPDATE ActualReviewers
INNER JOIN Artifacts art ON ActualReviewers.ArtifactId = art.ArtifactId
SET ActualReviewers.RepositoryId=art.RepositoryId;

/* Now that all AlgorithmRunners write the RepositoryId to ActualReviewers and
   all old NULL values are updated to the correct value, we can make RepositoryId a non-null column */

LOCK TABLES ActualReviewers WRITE;

ALTER TABLE `ActualReviewers` 
    DROP FOREIGN KEY FK_RepositoryActualReviewer;
ALTER TABLE ActualReviewers
	MODIFY COLUMN `RepositoryId` INT NOT NULL;
ALTER TABLE `ActualReviewers` 
	ADD CONSTRAINT `FK_RepositoryActualReviewer`
	  FOREIGN KEY (`RepositoryId`)
	  REFERENCES `Repositorys` (`RepositoryId`)
	  ON DELETE CASCADE
	  ON UPDATE NO ACTION;

UNLOCK TABLES;