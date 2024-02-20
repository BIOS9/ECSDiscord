-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: mariadb_ecs
-- Generation Time: Feb 20, 2024 at 10:41 AM
-- Server version: 10.11.6-MariaDB-log
-- PHP Version: 8.2.8

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `ecsdiscord`
--

-- --------------------------------------------------------

--
-- Table structure for table `autocreatepatterns`
--

CREATE TABLE `autocreatepatterns` (
  `id` int(11) NOT NULL,
  `pattern` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `coursealiases`
--

CREATE TABLE `coursealiases` (
  `id` int(11) NOT NULL,
  `name` varchar(32) DEFAULT NULL,
  `target` varchar(32) DEFAULT NULL,
  `hidden` tinyint(1) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `coursecategories`
--

CREATE TABLE `coursecategories` (
  `discordSnowflake` bigint(20) UNSIGNED NOT NULL,
  `autoImportPattern` varchar(256) DEFAULT NULL,
  `autoImportPriority` int(11) NOT NULL DEFAULT -1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `courses`
--

CREATE TABLE `courses` (
  `name` varchar(32) NOT NULL,
  `discordChannelSnowflake` bigint(20) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `pendingverifications`
--

CREATE TABLE `pendingverifications` (
  `token` varchar(32) NOT NULL,
  `encryptedUsername` varbinary(5000) NOT NULL,
  `discordSnowflake` bigint(20) UNSIGNED NOT NULL,
  `creationTime` bigint(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `servermessages`
--

CREATE TABLE `servermessages` (
  `messageID` bigint(20) UNSIGNED NOT NULL,
  `channelID` bigint(20) UNSIGNED NOT NULL,
  `content` text NOT NULL,
  `created` bigint(20) NOT NULL,
  `creator` bigint(20) UNSIGNED NOT NULL,
  `lastEdited` bigint(20) NOT NULL,
  `editor` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(64) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `usercourses`
--

CREATE TABLE `usercourses` (
  `userDiscordSnowflake` bigint(20) UNSIGNED NOT NULL,
  `courseName` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `discordSnowflake` bigint(20) UNSIGNED NOT NULL,
  `encryptedUsername` varbinary(5000) DEFAULT NULL,
  `disallowCourseJoin` tinyint(4) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `verificationhistory`
--

CREATE TABLE `verificationhistory` (
  `id` int(11) NOT NULL,
  `discordSnowflake` bigint(20) UNSIGNED NOT NULL,
  `encryptedUsername` varbinary(5000) NOT NULL,
  `verificationTime` bigint(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `verificationoverrides`
--

CREATE TABLE `verificationoverrides` (
  `discordSnowflake` bigint(20) UNSIGNED NOT NULL,
  `objectType` enum('ROLE','USER') NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `autocreatepatterns`
--
ALTER TABLE `autocreatepatterns`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `coursealiases`
--
ALTER TABLE `coursealiases`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `name_UNIQUE` (`name`);

--
-- Indexes for table `coursecategories`
--
ALTER TABLE `coursecategories`
  ADD PRIMARY KEY (`discordSnowflake`);

--
-- Indexes for table `courses`
--
ALTER TABLE `courses`
  ADD PRIMARY KEY (`name`),
  ADD UNIQUE KEY `discordChannelSnowflake` (`discordChannelSnowflake`);

--
-- Indexes for table `pendingverifications`
--
ALTER TABLE `pendingverifications`
  ADD PRIMARY KEY (`token`),
  ADD KEY `verificationDiscordSnowflake` (`discordSnowflake`);

--
-- Indexes for table `servermessages`
--
ALTER TABLE `servermessages`
  ADD PRIMARY KEY (`messageID`);

--
-- Indexes for table `usercourses`
--
ALTER TABLE `usercourses`
  ADD PRIMARY KEY (`userDiscordSnowflake`,`courseName`),
  ADD KEY `userCourseCourseName` (`courseName`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`discordSnowflake`);

--
-- Indexes for table `verificationhistory`
--
ALTER TABLE `verificationhistory`
  ADD PRIMARY KEY (`id`),
  ADD KEY `discordIdIndex` (`discordSnowflake`);

--
-- Indexes for table `verificationoverrides`
--
ALTER TABLE `verificationoverrides`
  ADD PRIMARY KEY (`discordSnowflake`),
  ADD KEY `TYPE` (`objectType`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `autocreatepatterns`
--
ALTER TABLE `autocreatepatterns`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `coursealiases`
--
ALTER TABLE `coursealiases`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `verificationhistory`
--
ALTER TABLE `verificationhistory`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `usercourses`
--
ALTER TABLE `usercourses`
  ADD CONSTRAINT `userCourseCourseName` FOREIGN KEY (`courseName`) REFERENCES `courses` (`name`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `userCourseDiscordSnowflake` FOREIGN KEY (`userDiscordSnowflake`) REFERENCES `users` (`discordSnowflake`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
