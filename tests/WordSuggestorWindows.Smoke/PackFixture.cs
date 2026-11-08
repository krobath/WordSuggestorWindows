using System.Runtime.InteropServices;

internal static class PackFixture
{
    public static void Create(string path, string language, int schema = 14, string major = "1", bool missingColumn = false)
    {
        if (sqlite3_open(path, out var db) != 0) throw new Exception("Fixture database open");
        try
        {
            var sql = $"""
                PRAGMA user_version={schema};
                CREATE TABLE terms(term_norm TEXT PRIMARY KEY, display TEXT, gram TEXT, flags INTEGER, freq INTEGER, lemma_norm TEXT);
                INSERT INTO terms VALUES('test','test','',0,1,'test');
                CREATE TABLE pack_meta(key TEXT PRIMARY KEY, value TEXT NOT NULL);
                INSERT INTO pack_meta VALUES
                ('pack_language','{language}'),('pack_version','1.0.0001'),('pack_version_major','{major}'),
                ('pack_version_minor','0'),('pack_version_build','1'),('pack_file_name','fixture.sqlite'),
                ('pack_created_at_iso8601','2026-11-08T00:00:00Z'),('pack_created_at_unix','1794096000'),('pack_sources_count','1');
                CREATE TABLE term_variants(variant_id INTEGER PRIMARY KEY, term_norm TEXT, display TEXT, gram_tag TEXT, pos TEXT, number TEXT, definiteness TEXT, tense TEXT, gender TEXT, flags INTEGER, freq INTEGER, lemma_norm TEXT, lemma_source TEXT, phonetic_key TEXT);
                CREATE TABLE term_analyses(term_norm TEXT, gram_tag TEXT, pos TEXT, number TEXT, definiteness TEXT, tense TEXT, gender TEXT, lemma_norm TEXT, lemma_source TEXT, PRIMARY KEY(term_norm,gram_tag));
                CREATE TABLE misspellings(misspelling_norm TEXT, corrected_norm TEXT);
                CREATE TABLE synonyms(term_norm TEXT, synonym_norm TEXT);
                CREATE TABLE bigrams(prev_norm TEXT, next_norm TEXT, count INTEGER);
                CREATE TABLE bigram_ctx(prev_norm TEXT, total INTEGER);
                CREATE TABLE trigrams(prev2_norm TEXT, prev1_norm TEXT, next_norm TEXT, count INTEGER);
                CREATE TABLE trigram_ctx(prev2_norm TEXT, prev1_norm TEXT, {(missingColumn ? "unexpected" : "total")} INTEGER);
                """;
            if (sqlite3_exec(db, sql, IntPtr.Zero, IntPtr.Zero, out var error) != 0)
            {
                sqlite3_free(error);
                throw new Exception("Fixture schema failed");
            }
        }
        finally { sqlite3_close(db); }
    }

    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_exec(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, IntPtr callback, IntPtr data, out IntPtr error);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_close(IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void sqlite3_free(IntPtr value);
}
