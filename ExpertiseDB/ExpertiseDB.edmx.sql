



-- -----------------------------------------------------------
-- Entity Designer DDL Script for MySQL Server 4.1 and higher
-- -----------------------------------------------------------
-- Date Created: 01/22/2014 16:57:09
-- Generated from EDMX file: D:\Programmieren\Repositories\ExpertiseExplorerWorking\ExpertiseExplorer\ExpertiseDB\ExpertiseDB.edmx
-- Target version: 3.0.0.0
-- --------------------------------------------------

DROP DATABASE IF EXISTS `expertisedb`;
CREATE DATABASE `expertisedb`;
USE `expertisedb`;

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

-- Creating table 'SourceRepositorys'

CREATE TABLE `SourceRepositorys` (
    `SourceRepositoryId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` varchar(1000)  NOT NULL,
    `URL` varchar(1000)  NOT NULL
);

-- Creating table 'Revisions'

CREATE TABLE `Revisions` (
    `RevisionId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `User` varchar(1000)  NOT NULL,
    `Tag` varchar(1000)  NULL,
    `Parent` varchar(1000)  NULL,
    `ID` varchar(1000)  NOT NULL,
    `Description` mediumtext  NULL,
    `Time` datetime  NOT NULL,
    `SourceRepositoryId` int  NOT NULL
);

-- Creating table 'FileRevisions'

CREATE TABLE `FileRevisions` (
    `FileRevisionId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `IsNew` bool  NOT NULL,
    `LinesAdded` int  NOT NULL,
    `LinesDeleted` int  NOT NULL,
    `Diff` mediumtext  NULL,
    `RevisionId` int  NOT NULL,
    `FilenameId` int  NOT NULL,
    `SourceRepositoryId` int  NOT NULL
);

-- Creating table 'Repositorys'

CREATE TABLE `Repositorys` (
    `RepositoryId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` varchar(1000)  NOT NULL,
    `SourceURL` varchar(1000)  NOT NULL,
    `LastUpdate` datetime  NULL
);

-- Creating table 'Developers'

CREATE TABLE `Developers` (
    `DeveloperId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` varchar(1000)  NOT NULL,
    `RepositoryId` int  NOT NULL
);

-- Creating table 'Algorithms'

CREATE TABLE `Algorithms` (
    `AlgorithmId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` varchar(1000)  NOT NULL,
    `GUID` CHAR(36) BINARY  NOT NULL
);

-- Creating table 'Artifacts'

CREATE TABLE `Artifacts` (
    `ArtifactId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `ArtifactTypeId` int  NOT NULL,
    `Name` varchar(1000)  NOT NULL,
    `RepositoryId` int  NOT NULL,
    `ParentArtifactId` int  NULL,
    `ModificationCount` int  NOT NULL
);

-- Creating table 'ArtifactTypes'

CREATE TABLE `ArtifactTypes` (
    `ArtifactTypeId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` varchar(1000)  NOT NULL
);

-- Creating table 'DeveloperExpertises'

CREATE TABLE `DeveloperExpertises` (
    `DeveloperExpertiseId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `DeveloperId` int  NOT NULL,
    `ArtifactId` int  NOT NULL,
    `DeliveriesCount` int  NOT NULL,
    `IsFirstAuthor` bool  NOT NULL,
    `Inferred` bool  NOT NULL
);

-- Creating table 'DeveloperExpertiseValues'

CREATE TABLE `DeveloperExpertiseValues` (
    `DeveloperExpertiseValueId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Value` double  NOT NULL,
    `DeveloperExpertiseId` int  NOT NULL,
    `AlgorithmId` int  NOT NULL
);

-- Creating table 'ActualReviewers'

CREATE TABLE `ActualReviewers` (
    `ActualReviewerId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Time` datetime  NOT NULL,
    `BugId` int  NOT NULL,
    `ActivityId` int  NOT NULL,
    `Reviewer` varchar(1000)  NOT NULL,
    `ArtifactId` int  NOT NULL
);

-- Creating table 'ComputedReviewers'

CREATE TABLE `ComputedReviewers` (
    `ComputedReviewerId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Expert1` varchar(1000)  NOT NULL,
    `Expert1Value` double  NOT NULL,
    `Expert2` varchar(1000)  NOT NULL,
    `Expert2Value` double  NOT NULL,
    `Expert3` varchar(1000)  NOT NULL,
    `Expert3Value` double  NOT NULL,
    `Expert4` varchar(1000)  NOT NULL,
    `Expert4Value` double  NOT NULL,
    `Expert5` varchar(1000)  NOT NULL,
    `Expert5Value` double  NOT NULL,
    `ActualReviewerId` int  NOT NULL,
    `AlgorithmId` int  NOT NULL
);

-- Creating table 'Filenames'

CREATE TABLE `Filenames` (
    `FilenameId` int AUTO_INCREMENT PRIMARY KEY NOT NULL,
    `Name` varchar(1000)  NOT NULL,
    `SourceRepositoryId` int  NOT NULL
);



-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------



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

-- Creating foreign key on `ArtifactId` in table 'ActualReviewers'

ALTER TABLE `ActualReviewers`
ADD CONSTRAINT `FK_ArtifactActualReviewer`
    FOREIGN KEY (`ArtifactId`)
    REFERENCES `Artifacts`
        (`ArtifactId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ArtifactActualReviewer'

CREATE INDEX `IX_FK_ArtifactActualReviewer` 
    ON `ActualReviewers`
    (`ArtifactId`);

-- Creating foreign key on `ActualReviewerId` in table 'ComputedReviewers'

ALTER TABLE `ComputedReviewers`
ADD CONSTRAINT `FK_ActualReviewerComputedReviewer`
    FOREIGN KEY (`ActualReviewerId`)
    REFERENCES `ActualReviewers`
        (`ActualReviewerId`)
    ON DELETE CASCADE ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ActualReviewerComputedReviewer'

CREATE INDEX `IX_FK_ActualReviewerComputedReviewer` 
    ON `ComputedReviewers`
    (`ActualReviewerId`);

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

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
