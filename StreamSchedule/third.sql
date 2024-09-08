CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Streams" (
    "StreamDate" TEXT NOT NULL CONSTRAINT "PK_Streams" PRIMARY KEY,
    "StreamTime" TEXT NOT NULL,
    "StreamTitle" TEXT NULL
);

CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NULL,
    "privileges" INTEGER NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240908141040_first', '8.0.8');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "Users" ADD "PreviousUsernames" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240908144445_second', '8.0.8');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240908150447_third', '8.0.8');

COMMIT;

