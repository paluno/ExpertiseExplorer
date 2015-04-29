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

