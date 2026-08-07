-- Generated reference copy of the current MySQL schema (synapse_db).
-- Source of truth is server/Migrations/ — do not hand-edit this file.
-- Regenerate with: dotnet ef migrations script --idempotent -o server/schema.sql

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260803130122_InitialCreate')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260803130122_InitialCreate', '10.0.9');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `Users` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` longtext NOT NULL,
        `Surname` longtext NOT NULL,
        `Email` varchar(256) NOT NULL,
        `Phone` longtext NOT NULL,
        `JobTitle` longtext NOT NULL,
        `Username` varchar(100) NOT NULL,
        `PasswordHash` longtext NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `Groups` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(200) NOT NULL,
        `OwnerId` int NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Groups_Users_OwnerId` FOREIGN KEY (`OwnerId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `UserSettings` (
        `UserId` int NOT NULL,
        `PostVisibility` int NOT NULL,
        `FavoritesVisibility` int NOT NULL,
        `CommentPermission` int NOT NULL,
        `PasswordExpiry` int NOT NULL,
        PRIMARY KEY (`UserId`),
        CONSTRAINT `FK_UserSettings_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `Posts` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Title` longtext NOT NULL,
        `AuthorId` int NOT NULL,
        `Description` longtext NOT NULL,
        `MiniDescription` longtext NOT NULL,
        `CreatedDate` date NOT NULL,
        `GroupId` int NULL,
        `Sends` int NOT NULL,
        `Downloads` int NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Posts_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `Groups` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Posts_Users_AuthorId` FOREIGN KEY (`AuthorId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `Comments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PostId` int NOT NULL,
        `AuthorId` int NOT NULL,
        `Text` longtext NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Comments_Posts_PostId` FOREIGN KEY (`PostId`) REFERENCES `Posts` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_Comments_Users_AuthorId` FOREIGN KEY (`AuthorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `NoteFiles` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PostId` int NOT NULL,
        `FileName` longtext NOT NULL,
        `FileUrl` longtext NOT NULL,
        `PageCount` int NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_NoteFiles_Posts_PostId` FOREIGN KEY (`PostId`) REFERENCES `Posts` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `PostFavorites` (
        `FavoritedByUsersId` int NOT NULL,
        `FavoritedPostsId` int NOT NULL,
        PRIMARY KEY (`FavoritedByUsersId`, `FavoritedPostsId`),
        CONSTRAINT `FK_PostFavorites_Posts_FavoritedPostsId` FOREIGN KEY (`FavoritedPostsId`) REFERENCES `Posts` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_PostFavorites_Users_FavoritedByUsersId` FOREIGN KEY (`FavoritedByUsersId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `PostLikes` (
        `LikedByUsersId` int NOT NULL,
        `LikedPostsId` int NOT NULL,
        PRIMARY KEY (`LikedByUsersId`, `LikedPostsId`),
        CONSTRAINT `FK_PostLikes_Posts_LikedPostsId` FOREIGN KEY (`LikedPostsId`) REFERENCES `Posts` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_PostLikes_Users_LikedByUsersId` FOREIGN KEY (`LikedByUsersId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE TABLE `PostReposts` (
        `RepostedByUsersId` int NOT NULL,
        `RepostedPostsId` int NOT NULL,
        PRIMARY KEY (`RepostedByUsersId`, `RepostedPostsId`),
        CONSTRAINT `FK_PostReposts_Posts_RepostedPostsId` FOREIGN KEY (`RepostedPostsId`) REFERENCES `Posts` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_PostReposts_Users_RepostedByUsersId` FOREIGN KEY (`RepostedByUsersId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_Comments_AuthorId` ON `Comments` (`AuthorId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_Comments_PostId` ON `Comments` (`PostId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE UNIQUE INDEX `IX_Groups_OwnerId_Name` ON `Groups` (`OwnerId`, `Name`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_NoteFiles_PostId` ON `NoteFiles` (`PostId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_PostFavorites_FavoritedPostsId` ON `PostFavorites` (`FavoritedPostsId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_PostLikes_LikedPostsId` ON `PostLikes` (`LikedPostsId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_PostReposts_RepostedPostsId` ON `PostReposts` (`RepostedPostsId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_Posts_AuthorId` ON `Posts` (`AuthorId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE INDEX `IX_Posts_GroupId` ON `Posts` (`GroupId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    CREATE UNIQUE INDEX `IX_Users_Username` ON `Users` (`Username`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260806093008_AddCoreEntities')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260806093008_AddCoreEntities', '10.0.9');
END;

COMMIT;
