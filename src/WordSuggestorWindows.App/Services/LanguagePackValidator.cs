using System.IO;
using System.Runtime.InteropServices;

namespace WordSuggestorWindows.App.Services;

public sealed record PackValidation(bool Compatible, string Code, string? Version = null, bool Legacy = false);

public static class LanguagePackValidator
{
    public static PackValidation Validate(string path, string languageTag, bool allowLegacy = false)
    {
        if (!File.Exists(path)) return new(false, "missing");
        IntPtr db = IntPtr.Zero;
        try
        {
            if (new FileInfo(path).Length < 100 || sqlite3_open_v2(path, out db, 1 | 0x8000, IntPtr.Zero) != 0)
                return new(false, "corrupt");
            if (Query(db, "PRAGMA quick_check(1)").SingleOrDefault() != "ok") return new(false, "corrupt");
            var columns = Query(db, "SELECT name FROM pragma_table_info('terms')").ToHashSet(StringComparer.Ordinal);
            if (new[] { "term_norm", "display", "gram", "flags", "freq", "lemma_norm" }.Any(c => !columns.Contains(c)))
                return new(false, "incompatible_schema");
            var schema = int.Parse(Query(db, "PRAGMA user_version").Single());
            var tables = Query(db, "SELECT name FROM sqlite_master WHERE type='table'").ToHashSet(StringComparer.Ordinal);
            if (!tables.Contains("pack_meta"))
                return allowLegacy && schema == 0 && languageTag == "da_DK"
                    ? new(true, "legacy_no_metadata", Legacy: true) : new(false, "missing_metadata");
            // Contract prose still says 9; the pinned Core PackBuilder emits 14.
            // These revisions retain the required query profile. New revisions
            // must be reviewed against Core before widening this boundary.
            if (schema is < 8 or > 14) return new(false, "incompatible_schema");
            var language = Query(db, "SELECT value FROM pack_meta WHERE key='pack_language'").SingleOrDefault();
            if (language != languageTag) return new(false, "wrong_language");
            var version = Query(db, "SELECT value FROM pack_meta WHERE key='pack_version'").SingleOrDefault();
            if (!System.Version.TryParse(version, out var parsedVersion) || parsedVersion.Build < 0) return new(false, "invalid_version");
            var requiredKeys = new[] { "pack_version_major", "pack_version_minor", "pack_version_build", "pack_file_name", "pack_created_at_iso8601", "pack_created_at_unix", "pack_sources_count" };
            var keys = Query(db, "SELECT key FROM pack_meta WHERE value <> ''").ToHashSet(StringComparer.Ordinal);
            if (requiredKeys.Any(k => !keys.Contains(k))) return new(false, "missing_metadata");
            var parts = new[] { "pack_version_major", "pack_version_minor", "pack_version_build" }
                .Select(key => Query(db, $"SELECT value FROM pack_meta WHERE key='{key}'").Single()).ToArray();
            if (!int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor) || !int.TryParse(parts[2], out var build) ||
                major != parsedVersion.Major || minor != parsedVersion.Minor || build != parsedVersion.Build)
                return new(false, "invalid_version");
            if (new[] { "term_variants", "term_analyses", "misspellings", "synonyms", "bigrams", "bigram_ctx", "trigrams", "trigram_ctx" }.Any(t => !tables.Contains(t)))
                return new(false, "incompatible_schema");
            var profile = new Dictionary<string, string[]>
            {
                ["term_variants"] = ["term_norm", "display", "gram_tag", "pos", "number", "definiteness", "tense", "gender", "flags", "freq", "lemma_norm", "lemma_source", "phonetic_key"],
                ["term_analyses"] = ["term_norm", "gram_tag", "pos", "number", "definiteness", "tense", "gender", "lemma_norm", "lemma_source"],
                ["misspellings"] = ["misspelling_norm", "corrected_norm"],
                ["synonyms"] = ["term_norm", "synonym_norm"],
                ["bigrams"] = ["prev_norm", "next_norm", "count"],
                ["bigram_ctx"] = ["prev_norm", "total"],
                ["trigrams"] = ["prev2_norm", "prev1_norm", "next_norm", "count"],
                ["trigram_ctx"] = ["prev2_norm", "prev1_norm", "total"]
            };
            foreach (var (table, required) in profile)
            {
                var present = Query(db, $"SELECT name FROM pragma_table_info('{table}')").ToHashSet(StringComparer.Ordinal);
                if (required.Any(column => !present.Contains(column))) return new(false, "incompatible_schema");
            }
            return new(true, "compatible", version);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or FormatException)
        { return new(false, "unreadable"); }
        finally { if (db != IntPtr.Zero) sqlite3_close(db); }
    }

    private static List<string> Query(IntPtr db, string sql)
    {
        if (sqlite3_prepare_v2(db, sql, -1, out var statement, IntPtr.Zero) != 0) throw new InvalidOperationException("Pack schema unavailable.");
        try
        {
            var values = new List<string>();
            int result;
            while ((result = sqlite3_step(statement)) == 100)
                values.Add(Marshal.PtrToStringUTF8(sqlite3_column_text(statement, 0)) ?? "");
            if (result != 101) throw new InvalidOperationException("Pack query failed.");
            return values;
        }
        finally { sqlite3_finalize(statement); }
    }

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr db, int flags, IntPtr vfs);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_close(IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int length, out IntPtr statement, IntPtr tail);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_step(IntPtr statement);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr sqlite3_column_text(IntPtr statement, int column);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_finalize(IntPtr statement);
}
