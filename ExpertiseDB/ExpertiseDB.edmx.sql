




-- -----------------------------------------------------------
-- Entity Designer DDL Script for MySQL Server 4.1 and higher
-- -----------------------------------------------------------
-- Date Created: 05/20/2015 16:55:34
-- Generated from EDMX file: d:\TestProgs\ExpertiseExplorer\ExpertiseDB\ExpertiseDB.edmx
-- Target version: 3.0.0.0
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- NOTE: if the constraint does not exist, an ignorable error will be reported.
-- --------------------------------------------------

--    ALTER TABLE `Revisions` DROP CONSTRAINT `FK_SourceRepositoryRevision`;
--    ALTER TABLE `FileRevisions` DROP CONSTRAINT `FK_RevisionFile`;
--    ALTER TABLE `Developers` DROP CONSTRAINT `FK_RepositoryDeveloper`;
--    ALTER TABLE `Artifacts` DROP CONSTRAINT `FK_ArtifactTypeArtifact`;
--    ALTER TABLE `Artifacts` DROP CONSTRAINT `FK_RepositoryArtifact`;
--    ALTER TABLE `DeveloperExpertises` DROP CONSTRAINT `FK_DeveloperDeveloperExpertise`;
--    ALTER TABLE `DeveloperExpertises` DROP CONSTRAINT `FK_DeveloperExpertiseArtifact`;
--    ALTER TABLE `Artifacts` DROP CONSTRAINT `FK_ArtifactArtifact`;
--    ALTER TABLE `DeveloperExpertiseValues` DROP CONSTRAINT `FK_DeveloperExpertiseDeveloperExpertiseValue`;
--    ALTER TABLE `DeveloperExpertiseValues` DROP CONSTRAINT `FK_DeveloperExpertiseValueAlgorithm`;
--    ALTER TABLE `ActualReviewers` DROP CONSTRAINT `FK_ArtifactActualReviewer`;
--    ALTER TABLE `ActualReviewers` DROP CONSTRAINT `FK_RepositoryActualReviewer`;
--    ALTER TABLE `ComputedReviewers` DROP CONSTRAINT `FK_ActualReviewerComputedReviewer`;
--    ALTER TABLE `FileRevisions` DROP CONSTRAINT `FK_FilenameFileRevision`;
--    ALTER TABLE `FileRevisions` DROP CONSTRAINT `FK_SourceRepositoryFileRevision`;
--    ALTER TABLE `Filenames` DROP CONSTRAINT `FK_SourceRepositoryFilename`;

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------
SET foreign_key_checks = 0;
    DROP TABLE IF EXISTS `SourceRepositorys`;
    DROP TABLE IF EXISTS `Revisions`;
    DROP TABLE IF EXISTS `FileRevisions`;
    DROP TABLE IF EXISTS `Repositorys`;
    DROP TABLE IF EXISTS `Developers`;
    DROP TABLE IF EXISTS `Algorithms`;
    DROP TABLE IF EXISTS `Artifacts`;
    DROP TABLE IF EXISTS `ArtifactTypes`;
    DROP TABLE IF EXISTS `DeveloperExpertises`;
    DROP TABLE IF EXISTS `DeveloperExpertiseValues`;
    DROP TABLE IF EXISTS `ActualReviewers`;
    DROP TABLE IF EXISTS `ComputedReviewers`;
    DROP TABLE IF EXISTS `Filenames`;
SET foreign_key_checks = 1;

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

CREATE TABLE `SourceRepositorys`(
	`SourceRepositoryId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Name` longtext NOT NULL, 
	`URL` longtext NOT NULL);

ALTER TABLE `SourceRepositorys` ADD PRIMARY KEY (SourceRepositoryId);




CREATE TABLE `Revisions`(
	`RevisionId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`User` longtext NOT NULL, 
	`Tag` longtext, 
	`Parent` longtext, 
	`ID` longtext NOT NULL, 
	`Description` longtext, 
	`Time` datetime NOT NULL, 
	`SourceRepositoryId` int NOT NULL);

ALTER TABLE `Revisions` ADD PRIMARY KEY (RevisionId);




CREATE TABLE `FileRevisions`(
	`FileRevisionId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`IsNew` bool NOT NULL, 
	`LinesAdded` int NOT NULL, 
	`LinesDeleted` int NOT NULL, 
	`Diff` longtext, 
	`RevisionId` int NOT NULL, 
	`FilenameId` int NOT NULL, 
	`SourceRepositoryId` int NOT NULL);

ALTER TABLE `FileRevisions` ADD PRIMARY KEY (FileRevisionId);




CREATE TABLE `Repositorys`(
	`RepositoryId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Name` longtext NOT NULL, 
	`SourceURL` longtext NOT NULL, 
	`LastUpdate` datetime);

ALTER TABLE `Repositorys` ADD PRIMARY KEY (RepositoryId);




CREATE TABLE `Developers`(
	`DeveloperId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Name` longtext NOT NULL, 
	`RepositoryId` int NOT NULL);

ALTER TABLE `Developers` ADD PRIMARY KEY (DeveloperId);




CREATE TABLE `Algorithms`(
	`AlgorithmId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Name` longtext NOT NULL, 
	`GUID` CHAR(36) BINARY NOT NULL);

ALTER TABLE `Algorithms` ADD PRIMARY KEY (AlgorithmId);




CREATE TABLE `Artifacts`(
	`ArtifactId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`ArtifactTypeId` int NOT NULL, 
	`Name` longtext NOT NULL, 
	`RepositoryId` int NOT NULL, 
	`ParentArtifactId` int, 
	`ModificationCount` int NOT NULL);

ALTER TABLE `Artifacts` ADD PRIMARY KEY (ArtifactId);




CREATE TABLE `ArtifactTypes`(
	`ArtifactTypeId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Name` longtext NOT NULL);

ALTER TABLE `ArtifactTypes` ADD PRIMARY KEY (ArtifactTypeId);




CREATE TABLE `DeveloperExpertises`(
	`DeveloperExpertiseId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`DeveloperId` int NOT NULL, 
	`ArtifactId` int NOT NULL, 
	`DeliveriesCount` int NOT NULL, 
	`IsFirstAuthor` bool NOT NULL, 
	`Inferred` bool NOT NULL);

ALTER TABLE `DeveloperExpertises` ADD PRIMARY KEY (DeveloperExpertiseId);




CREATE TABLE `DeveloperExpertiseValues`(
	`DeveloperExpertiseValueId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Value` double NOT NULL, 
	`DeveloperExpertiseId` int NOT NULL, 
	`AlgorithmId` int NOT NULL);

ALTER TABLE `DeveloperExpertiseValues` ADD PRIMARY KEY (DeveloperExpertiseValueId);




CREATE TABLE `ActualReviewers`(
	`ActualReviewerId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`ActivityId` int NOT NULL, 
	`Reviewer` longtext NOT NULL, 
	`RepositoryId` int NOT NULL, 
	`BugId` int NOT NULL);

ALTER TABLE `ActualReviewers` ADD PRIMARY KEY (ActualReviewerId);




CREATE TABLE `ComputedReviewers`(
	`ComputedReviewerId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Expert1` longtext NOT NULL, 
	`Expert1Value` double NOT NULL, 
	`Expert2` longtext NOT NULL, 
	`Expert2Value` double NOT NULL, 
	`Expert3` longtext NOT NULL, 
	`Expert3Value` double NOT NULL, 
	`Expert4` longtext NOT NULL, 
	`Expert4Value` double NOT NULL, 
	`Expert5` longtext NOT NULL, 
	`Expert5Value` double NOT NULL, 
	`ActualReviewerId` int NOT NULL, 
	`AlgorithmId` int NOT NULL, 
	`BugId` int NOT NULL);

ALTER TABLE `ComputedReviewers` ADD PRIMARY KEY (ComputedReviewerId);




CREATE TABLE `Filenames`(
	`FilenameId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Name` longtext NOT NULL, 
	`SourceRepositoryId` int NOT NULL);

ALTER TABLE `Filenames` ADD PRIMARY KEY (FilenameId);




CREATE TABLE `Bugs`(
	`BugId` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`ChangeId` longtext NOT NULL);

ALTER TABLE `Bugs` ADD PRIMARY KEY (BugId);




CREATE TABLE `RepositoryAlgorithmRunStatus`(
	`RepositoryRepositoryId` int NOT NULL, 
	`AlgorithmAlgorithmId` int NOT NULL, 
	`RunUntil` datetime NOT NULL);

ALTER TABLE `RepositoryAlgorithmRunStatus` ADD PRIMARY KEY (AlgorithmAlgorithmId, RepositoryRepositoryId);




CREATE TABLE `ArtifactBugRelation`(
	`Artifacts_ArtifactId` int NOT NULL, 
	`Bugs_BugId` int NOT NULL);

ALTER TABLE `ArtifactBugRelation` ADD PRIMARY KEY (Artifacts_ArtifactId, Bugs_BugId);






-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on `SourceRepositoryId` in table 'Revisions'

ALTER TABLE `Revisions`
ADD CONSTRAINT `FK_SourceRepositoryRevision`
    FOREIGN KEY (`SourceRepositoryId`)
    REFERENCES `SourceRepositorys`
        (`SourceRepositoryId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_SourceRepositoryRevision'

CREATE INDEX `IX_FK_SourceRepositoryRevision` 
    ON `Revisions`
    (`SourceRepositoryId`);

-- Creating foreign key on `RevisionId` in table 'FileRevisions'

ALTER TABLE `FileRevisions`
ADD CONSTRAINT `FK_RevisionFile`
    FOREIGN KEY (`RevisionId`)
    REFERENCES `Revisions`
        (`RevisionId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RevisionFile'

CREATE INDEX `IX_FK_RevisionFile` 
    ON `FileRevisions`
    (`RevisionId`);

-- Creating foreign key on `RepositoryId` in table 'Developers'

ALTER TABLE `Developers`
ADD CONSTRAINT `FK_RepositoryDeveloper`
    FOREIGN KEY (`RepositoryId`)
    REFERENCES `Repositorys`
        (`RepositoryId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RepositoryDeveloper'

CREATE INDEX `IX_FK_RepositoryDeveloper` 
    ON `Developers`
    (`RepositoryId`);

-- Creating foreign key on `ArtifactTypeId` in table 'Artifacts'

ALTER TABLE `Artifacts`
ADD CONSTRAINT `FK_ArtifactTypeArtifact`
    FOREIGN KEY (`ArtifactTypeId`)
    REFERENCES `ArtifactTypes`
        (`ArtifactTypeId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ArtifactTypeArtifact'

CREATE INDEX `IX_FK_ArtifactTypeArtifact` 
    ON `Artifacts`
    (`ArtifactTypeId`);

-- Creating foreign key on `RepositoryId` in table 'Artifacts'

ALTER TABLE `Artifacts`
ADD CONSTRAINT `FK_RepositoryArtifact`
    FOREIGN KEY (`RepositoryId`)
    REFERENCES `Repositorys`
        (`RepositoryId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RepositoryArtifact'

CREATE INDEX `IX_FK_RepositoryArtifact` 
    ON `Artifacts`
    (`RepositoryId`);

-- Creating foreign key on `DeveloperId` in table 'DeveloperExpertises'

ALTER TABLE `DeveloperExpertises`
ADD CONSTRAINT `FK_DeveloperDeveloperExpertise`
    FOREIGN KEY (`DeveloperId`)
    REFERENCES `Developers`
        (`DeveloperId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DeveloperDeveloperExpertise'

CREATE INDEX `IX_FK_DeveloperDeveloperExpertise` 
    ON `DeveloperExpertises`
    (`DeveloperId`);

-- Creating foreign key on `ArtifactId` in table 'DeveloperExpertises'

ALTER TABLE `DeveloperExpertises`
ADD CONSTRAINT `FK_DeveloperExpertiseArtifact`
    FOREIGN KEY (`ArtifactId`)
    REFERENCES `Artifacts`
        (`ArtifactId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DeveloperExpertiseArtifact'

CREATE INDEX `IX_FK_DeveloperExpertiseArtifact` 
    ON `DeveloperExpertises`
    (`ArtifactId`);

-- Creating foreign key on `ParentArtifactId` in table 'Artifacts'

ALTER TABLE `Artifacts`
ADD CONSTRAINT `FK_ArtifactArtifact`
    FOREIGN KEY (`ParentArtifactId`)
    REFERENCES `Artifacts`
        (`ArtifactId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ArtifactArtifact'

CREATE INDEX `IX_FK_ArtifactArtifact` 
    ON `Artifacts`
    (`ParentArtifactId`);

-- Creating foreign key on `DeveloperExpertiseId` in table 'DeveloperExpertiseValues'

ALTER TABLE `DeveloperExpertiseValues`
ADD CONSTRAINT `FK_DeveloperExpertiseDeveloperExpertiseValue`
    FOREIGN KEY (`DeveloperExpertiseId`)
    REFERENCES `DeveloperExpertises`
        (`DeveloperExpertiseId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DeveloperExpertiseDeveloperExpertiseValue'

CREATE INDEX `IX_FK_DeveloperExpertiseDeveloperExpertiseValue` 
    ON `DeveloperExpertiseValues`
    (`DeveloperExpertiseId`);

-- Creating foreign key on `AlgorithmId` in table 'DeveloperExpertiseValues'

ALTER TABLE `DeveloperExpertiseValues`
ADD CONSTRAINT `FK_DeveloperExpertiseValueAlgorithm`
    FOREIGN KEY (`AlgorithmId`)
    REFERENCES `Algorithms`
        (`AlgorithmId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DeveloperExpertiseValueAlgorithm'

CREATE INDEX `IX_FK_DeveloperExpertiseValueAlgorithm` 
    ON `DeveloperExpertiseValues`
    (`AlgorithmId`);

-- Creating foreign key on `RepositoryId` in table 'ActualReviewers'

ALTER TABLE `ActualReviewers`
ADD CONSTRAINT `FK_RepositoryActualReviewer`
    FOREIGN KEY (`RepositoryId`)
    REFERENCES `Repositorys`
        (`RepositoryId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RepositoryActualReviewer'

CREATE INDEX `IX_FK_RepositoryActualReviewer` 
    ON `ActualReviewers`
    (`RepositoryId`);

-- Creating foreign key on `FilenameId` in table 'FileRevisions'

ALTER TABLE `FileRevisions`
ADD CONSTRAINT `FK_FilenameFileRevision`
    FOREIGN KEY (`FilenameId`)
    REFERENCES `Filenames`
        (`FilenameId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_FilenameFileRevision'

CREATE INDEX `IX_FK_FilenameFileRevision` 
    ON `FileRevisions`
    (`FilenameId`);

-- Creating foreign key on `SourceRepositoryId` in table 'FileRevisions'

ALTER TABLE `FileRevisions`
ADD CONSTRAINT `FK_SourceRepositoryFileRevision`
    FOREIGN KEY (`SourceRepositoryId`)
    REFERENCES `SourceRepositorys`
        (`SourceRepositoryId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_SourceRepositoryFileRevision'

CREATE INDEX `IX_FK_SourceRepositoryFileRevision` 
    ON `FileRevisions`
    (`SourceRepositoryId`);

-- Creating foreign key on `SourceRepositoryId` in table 'Filenames'

ALTER TABLE `Filenames`
ADD CONSTRAINT `FK_SourceRepositoryFilename`
    FOREIGN KEY (`SourceRepositoryId`)
    REFERENCES `SourceRepositorys`
        (`SourceRepositoryId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_SourceRepositoryFilename'

CREATE INDEX `IX_FK_SourceRepositoryFilename` 
    ON `Filenames`
    (`SourceRepositoryId`);

-- Creating foreign key on `Artifacts_ArtifactId` in table 'ArtifactBugRelation'

ALTER TABLE `ArtifactBugRelation`
ADD CONSTRAINT `FK_ArtifactBugRelation_Artifact`
    FOREIGN KEY (`Artifacts_ArtifactId`)
    REFERENCES `Artifacts`
        (`ArtifactId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating foreign key on `Bugs_BugId` in table 'ArtifactBugRelation'

ALTER TABLE `ArtifactBugRelation`
ADD CONSTRAINT `FK_ArtifactBugRelation_Bug`
    FOREIGN KEY (`Bugs_BugId`)
    REFERENCES `Bugs`
        (`BugId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ArtifactBugRelation_Bug'

CREATE INDEX `IX_FK_ArtifactBugRelation_Bug` 
    ON `ArtifactBugRelation`
    (`Bugs_BugId`);

-- Creating foreign key on `BugId` in table 'ActualReviewers'

ALTER TABLE `ActualReviewers`
ADD CONSTRAINT `FK_BugActualReviewerRelation`
    FOREIGN KEY (`BugId`)
    REFERENCES `Bugs`
        (`BugId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_BugActualReviewerRelation'

CREATE INDEX `IX_FK_BugActualReviewerRelation` 
    ON `ActualReviewers`
    (`BugId`);

-- Creating foreign key on `BugId` in table 'ComputedReviewers'

ALTER TABLE `ComputedReviewers`
ADD CONSTRAINT `FK_BugComputedReviewerRelation`
    FOREIGN KEY (`BugId`)
    REFERENCES `Bugs`
        (`BugId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_BugComputedReviewerRelation'

CREATE INDEX `IX_FK_BugComputedReviewerRelation` 
    ON `ComputedReviewers`
    (`BugId`);

-- Creating foreign key on `RepositoryRepositoryId` in table 'RepositoryAlgorithmRunStatus'

ALTER TABLE `RepositoryAlgorithmRunStatus`
ADD CONSTRAINT `FK_RepositoryAlgorithmRunStatusRepository`
    FOREIGN KEY (`RepositoryRepositoryId`)
    REFERENCES `Repositorys`
        (`RepositoryId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_RepositoryAlgorithmRunStatusRepository'

CREATE INDEX `IX_FK_RepositoryAlgorithmRunStatusRepository` 
    ON `RepositoryAlgorithmRunStatus`
    (`RepositoryRepositoryId`);

-- Creating foreign key on `AlgorithmAlgorithmId` in table 'RepositoryAlgorithmRunStatus'

ALTER TABLE `RepositoryAlgorithmRunStatus`
ADD CONSTRAINT `FK_RepositoryAlgorithmRunStatusAlgorithm`
    FOREIGN KEY (`AlgorithmAlgorithmId`)
    REFERENCES `Algorithms`
        (`AlgorithmId`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
