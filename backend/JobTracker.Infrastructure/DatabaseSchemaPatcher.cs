using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobTracker.Infrastructure;

/// <summary>
/// Applies idempotent SQL patches for databases created before the current model.
/// EnsureCreated() does not alter existing databases, which causes 500s on new columns/tables.
/// </summary>
public static class DatabaseSchemaPatcher
{
    public static async Task ApplyAsync(AppDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var patches = new[]
        {
            """
            ALTER TABLE "Applications" ADD COLUMN IF NOT EXISTS "ResumeId" uuid NULL;
            """,
            """
            ALTER TABLE "Applications" ADD COLUMN IF NOT EXISTS "JobDescription" character varying(2000) NOT NULL DEFAULT '';
            """,
            """
            UPDATE "Applications" SET "Status" = 0
            WHERE "Status" IS NULL OR "Status" < 0 OR "Status" > 4;
            """,
            """
            ALTER TABLE "Resumes" ADD COLUMN IF NOT EXISTS "Name" character varying(200) NOT NULL DEFAULT '';
            """,
            """
            ALTER TABLE "Resumes" ADD COLUMN IF NOT EXISTS "FilePath" character varying(512) NOT NULL DEFAULT '';
            """,
            """
            ALTER TABLE "Resumes" ADD COLUMN IF NOT EXISTS "ExtractedText" text NOT NULL DEFAULT '';
            """,
            """
            ALTER TABLE "Resumes" ADD COLUMN IF NOT EXISTS "UploadedAt" timestamp with time zone NOT NULL DEFAULT NOW();
            """,
            """
            DO $patch$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'Resumes' AND column_name = 'FileName'
              ) THEN
                UPDATE "Resumes"
                SET "Name" = "FileName"
                WHERE "Name" IS NULL OR "Name" = '';
              END IF;
            END
            $patch$;
            """,
            """
            CREATE TABLE IF NOT EXISTS "AIJobs" (
                "Id" uuid NOT NULL,
                "ApplicationId" uuid NOT NULL,
                "JobType" character varying(128) NOT NULL,
                "Status" integer NOT NULL,
                "Result" character varying(4000) NOT NULL DEFAULT '',
                "CreatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_AIJobs" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_AIJobs_Applications_ApplicationId" FOREIGN KEY ("ApplicationId")
                    REFERENCES "Applications" ("Id") ON DELETE CASCADE
            );
            """,
            """
            CREATE INDEX IF NOT EXISTS "IX_AIJobs_ApplicationId" ON "AIJobs" ("ApplicationId");
            """
        };

        foreach (var sql in patches)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database patch skipped or failed (table/column may not exist yet): {Sql}", sql.Trim());
            }
        }

        logger.LogInformation("Database schema patches applied.");
    }
}
