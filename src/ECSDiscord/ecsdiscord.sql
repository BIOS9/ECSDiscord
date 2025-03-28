/*M!999999\- enable the sandbox mode */
-- MariaDB dump 10.19-11.4.5-MariaDB, for Linux (x86_64)
--
-- Host: localhost    Database: ecsdiscord
-- ------------------------------------------------------
-- Server version       11.4.5-MariaDB-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*M!100616 SET @OLD_NOTE_VERBOSITY=@@NOTE_VERBOSITY, NOTE_VERBOSITY=0 */;

--
-- Table structure for table `autocreatepatterns`
--

DROP TABLE IF EXISTS `autocreatepatterns`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `autocreatepatterns` (
                                      `id` int(11) NOT NULL AUTO_INCREMENT,
                                      `pattern` varchar(32) NOT NULL,
                                      PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `coursealiases`
--

DROP TABLE IF EXISTS `coursealiases`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `coursealiases` (
                                 `id` int(11) NOT NULL AUTO_INCREMENT,
                                 `name` varchar(32) DEFAULT NULL,
                                 `target` varchar(32) DEFAULT NULL,
                                 `hidden` tinyint(1) DEFAULT NULL,
                                 PRIMARY KEY (`id`),
                                 UNIQUE KEY `name_UNIQUE` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `coursecategories`
--

DROP TABLE IF EXISTS `coursecategories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `coursecategories` (
                                    `discordSnowflake` bigint(20) unsigned NOT NULL,
                                    `autoImportPattern` varchar(256) DEFAULT NULL,
                                    `autoImportPriority` int(11) NOT NULL DEFAULT -1,
                                    PRIMARY KEY (`discordSnowflake`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `courses`
--

DROP TABLE IF EXISTS `courses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `courses` (
                           `name` varchar(32) NOT NULL,
                           `discordChannelSnowflake` bigint(20) unsigned NOT NULL,
                           PRIMARY KEY (`name`),
                           UNIQUE KEY `discordChannelSnowflake` (`discordChannelSnowflake`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mcaccounts`
--

DROP TABLE IF EXISTS `mcaccounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `mcaccounts` (
                              `discordSnowflake` bigint(20) unsigned NOT NULL,
                              `minecraftUuid` uuid NOT NULL,
                              `creationTime` bigint(20) NOT NULL,
                              `isExternal` tinyint(1) NOT NULL,
                              PRIMARY KEY (`minecraftUuid`),
                              KEY `mcaccounts_users_discordSnowflake_fk` (`discordSnowflake`),
                              CONSTRAINT `mcaccounts_users_discordSnowflake_fk` FOREIGN KEY (`discordSnowflake`) REFERENCES `users` (`discordSnowflake`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pendingverifications`
--

DROP TABLE IF EXISTS `pendingverifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `pendingverifications` (
                                        `token` varchar(32) NOT NULL,
                                        `encryptedUsername` varbinary(5000) NOT NULL,
                                        `discordSnowflake` bigint(20) unsigned NOT NULL,
                                        `creationTime` bigint(20) NOT NULL,
                                        PRIMARY KEY (`token`),
                                        KEY `verificationDiscordSnowflake` (`discordSnowflake`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `servermessages`
--

DROP TABLE IF EXISTS `servermessages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `servermessages` (
                                  `messageID` bigint(20) unsigned NOT NULL,
                                  `channelID` bigint(20) unsigned NOT NULL,
                                  `content` text NOT NULL,
                                  `created` bigint(20) NOT NULL,
                                  `creator` bigint(20) unsigned NOT NULL,
                                  `lastEdited` bigint(20) NOT NULL,
                                  `editor` bigint(20) unsigned NOT NULL,
                                  `name` varchar(64) NOT NULL,
                                  PRIMARY KEY (`messageID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usercourses`
--

DROP TABLE IF EXISTS `usercourses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `usercourses` (
                               `userDiscordSnowflake` bigint(20) unsigned NOT NULL,
                               `courseName` varchar(32) NOT NULL,
                               PRIMARY KEY (`userDiscordSnowflake`,`courseName`),
                               KEY `userCourseCourseName` (`courseName`),
                               CONSTRAINT `userCourseCourseName` FOREIGN KEY (`courseName`) REFERENCES `courses` (`name`) ON DELETE CASCADE ON UPDATE CASCADE,
                               CONSTRAINT `userCourseDiscordSnowflake` FOREIGN KEY (`userDiscordSnowflake`) REFERENCES `users` (`discordSnowflake`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
                         `discordSnowflake` bigint(20) unsigned NOT NULL,
                         `encryptedUsername` varbinary(5000) DEFAULT NULL,
                         `disallowCourseJoin` tinyint(4) NOT NULL DEFAULT 0,
                         PRIMARY KEY (`discordSnowflake`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `verificationhistory`
--

DROP TABLE IF EXISTS `verificationhistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `verificationhistory` (
                                       `id` int(11) NOT NULL AUTO_INCREMENT,
                                       `discordSnowflake` bigint(20) unsigned NOT NULL,
                                       `encryptedUsername` varbinary(5000) NOT NULL,
                                       `verificationTime` bigint(20) NOT NULL,
                                       PRIMARY KEY (`id`),
                                       KEY `discordIdIndex` (`discordSnowflake`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `verificationoverrides`
--

DROP TABLE IF EXISTS `verificationoverrides`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `verificationoverrides` (
                                         `discordSnowflake` bigint(20) unsigned NOT NULL,
                                         `objectType` enum('ROLE','USER') NOT NULL,
                                         PRIMARY KEY (`discordSnowflake`),
                                         KEY `TYPE` (`objectType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*M!100616 SET NOTE_VERBOSITY=@OLD_NOTE_VERBOSITY */;

-- Dump completed on 2025-03-29  0:58:53