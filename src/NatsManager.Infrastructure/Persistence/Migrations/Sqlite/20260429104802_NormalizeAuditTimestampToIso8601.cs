using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NatsManager.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <summary>
    /// Drops the legacy <c>HasConversion&lt;string&gt;()</c> on <c>AuditEvents.Timestamp</c> and
    /// switches to the EF default <see cref="System.DateTimeOffset"/> mapping. The SQLite column
    /// type remains <c>TEXT</c>, so this migration is a SQL no-op for the schema; what changes is
    /// the on-disk text format.
    /// <para>
    /// Pre-migration values were produced by <see cref="System.DateTimeOffset.ToString()"/> with
    /// invariant culture, e.g. <c>04/29/2026 10:43:06 +00:00</c>. EF Core's default Sqlite
    /// type mapping reads / writes ISO 8601 (<c>yyyy-MM-dd HH:mm:ss.FFFFFFFzzz</c>), so existing
    /// rows must be reformatted in place or they will fail to parse on the next read.
    /// </para>
    /// </summary>
    public partial class NormalizeAuditTimestampToIso8601 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert "MM/dd/yyyy HH:mm:ss ±HH:MM" → "yyyy-MM-dd HH:mm:ss±HH:MM".
            // The WHERE clause makes this idempotent: rows already in ISO 8601 (where char 3
            // and 6 are not '/') are left untouched, so re-applying the migration after a
            // partial rollout is safe.
            migrationBuilder.Sql(
                """
                UPDATE AuditEvents
                SET Timestamp =
                    substr(Timestamp, 7, 4) || '-' ||
                    substr(Timestamp, 1, 2) || '-' ||
                    substr(Timestamp, 4, 2) || ' ' ||
                    substr(Timestamp, 12, 8) ||
                    substr(Timestamp, 21)
                WHERE substr(Timestamp, 3, 1) = '/' AND substr(Timestamp, 6, 1) = '/';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert ISO 8601 strings back to the legacy "MM/dd/yyyy HH:mm:ss ±HH:MM" format.
            // Sub-second precision present in ISO 8601 values is intentionally truncated, since
            // the legacy format never carried fractional seconds. The space before the offset
            // matches the original DateTimeOffset.ToString() output.
            migrationBuilder.Sql(
                """
                UPDATE AuditEvents
                SET Timestamp =
                    substr(Timestamp, 6, 2) || '/' ||
                    substr(Timestamp, 9, 2) || '/' ||
                    substr(Timestamp, 1, 4) || ' ' ||
                    substr(Timestamp, 12, 8) || ' ' ||
                    substr(Timestamp, length(Timestamp) - 5, 6)
                WHERE substr(Timestamp, 5, 1) = '-' AND substr(Timestamp, 8, 1) = '-';
                """);
        }
    }
}
