using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHeaderNotificationStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[HeaderNotificationStates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [HeaderNotificationStates] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] uniqueidentifier NOT NULL,
                        [NotificationId] nvarchar(200) NOT NULL,
                        [IsRead] bit NOT NULL,
                        [IsDismissed] bit NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [ReadAtUtc] datetime2 NULL,
                        [DismissedAtUtc] datetime2 NULL,
                        [UpdatedAtUtc] datetime2 NULL,
                        CONSTRAINT [PK_HeaderNotificationStates] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_HeaderNotificationStates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_HeaderNotificationStates_UserId_NotificationId'
                      AND object_id = OBJECT_ID(N'[HeaderNotificationStates]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_HeaderNotificationStates_UserId_NotificationId]
                    ON [HeaderNotificationStates] ([UserId], [NotificationId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[HeaderNotificationStates]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [HeaderNotificationStates];
                END
                """);
        }
    }
}
