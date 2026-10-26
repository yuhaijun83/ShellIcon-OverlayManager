using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text;
using ShellIconOverlayManager.Models;

namespace ShellIconOverlayManager.Data
{
    /// <summary>
    /// SQLite 数据访问层：保存用户设置（语言等）与删除/修改操作履历。
    /// </summary>
    public static class AppDatabase
    {
        private const string DbFileName = "ShellIcon-OverlayManager.db";
        private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string FallbackFolderName = "ShellIconOverlayManager";

        public static string DataDirectory { get; private set; }
        public static string DbPath { get; private set; }

        /// <summary>程序目录不可写时，数据库自动回退到 %LOCALAPPDATA%。</summary>
        public static bool UsingFallbackLocation { get; private set; }

        static AppDatabase()
        {
            // 按开发要求：db 文件放在应用程序的安装目录下
            DataDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DbPath = Path.Combine(DataDirectory, DbFileName);
        }

        public static void Initialize()
        {
            if (!CanWriteTo(DataDirectory))
            {
                // 安装在 Program Files 等只读目录时，回退到用户目录，保证功能可用
                var fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    FallbackFolderName);
                if (CanWriteTo(fallback))
                {
                    DataDirectory = fallback;
                    DbPath = Path.Combine(DataDirectory, DbFileName);
                    UsingFallbackLocation = true;
                }
            }

            Directory.CreateDirectory(DataDirectory);

            using (var connection = Open())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
CREATE TABLE IF NOT EXISTS Settings (
    Key   TEXT PRIMARY KEY NOT NULL,
    Value TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS DeleteHistory (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Root        INTEGER NOT NULL,
    KeyName     TEXT NOT NULL,
    Operation   INTEGER NOT NULL,
    OldKeyName  TEXT,
    NewKeyName  TEXT,
    Summary     TEXT,
    OperatedAt  TEXT NOT NULL,
    IsRestored  INTEGER NOT NULL DEFAULT 0,
    RestoredAt  TEXT
);

CREATE TABLE IF NOT EXISTS DeleteHistoryValues (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    HistoryId  INTEGER NOT NULL,
    ValueName  TEXT NOT NULL,
    ValueKind  INTEGER NOT NULL,
    ValueData  TEXT
);

CREATE INDEX IF NOT EXISTS IX_DeleteHistory_OperatedAt ON DeleteHistory(OperatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_DeleteHistoryValues_HistoryId ON DeleteHistoryValues(HistoryId);
";
                    command.ExecuteNonQuery();
                }
            }
        }

        private static SQLiteConnection Open()
        {
            var connection = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;");
            connection.Open();
            return connection;
        }

        private static bool CanWriteTo(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var probe = Path.Combine(directory, ".write-test.tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region 设置

        public static string GetSetting(string key, string defaultValue)
        {
            try
            {
                using (var connection = Open())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Value FROM Settings WHERE Key = @key;";
                    command.Parameters.AddWithValue("@key", key);
                    var value = command.ExecuteScalar() as string;
                    return value ?? defaultValue;
                }
            }
            catch (SQLiteException)
            {
                return defaultValue;
            }
        }

        public static void SetSetting(string key, string value)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO Settings(Key, Value) VALUES(@key, @value) " +
                                      "ON CONFLICT(Key) DO UPDATE SET Value = @value;";
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@value", value ?? string.Empty);
                command.ExecuteNonQuery();
            }
        }

        #endregion

        #region 操作履历

        public static void AddHistory(HistoryEntry entry)
        {
            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            {
                long id;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
INSERT INTO DeleteHistory(Root, KeyName, Operation, OldKeyName, NewKeyName, Summary, OperatedAt, IsRestored, RestoredAt)
VALUES(@root, @keyName, @operation, @oldKeyName, @newKeyName, @summary, @operatedAt, 0, NULL);
SELECT last_insert_rowid();";
                    command.Parameters.AddWithValue("@root", (int)entry.Root);
                    command.Parameters.AddWithValue("@keyName", entry.KeyName ?? string.Empty);
                    command.Parameters.AddWithValue("@operation", (int)entry.Operation);
                    command.Parameters.AddWithValue("@oldKeyName", (object)entry.OldKeyName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@newKeyName", (object)entry.NewKeyName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@summary", entry.Summary ?? string.Empty);
                    command.Parameters.AddWithValue("@operatedAt", FormatTime(entry.OperatedAt));
                    id = Convert.ToInt64(command.ExecuteScalar());
                }

                if (entry.Values != null)
                {
                    foreach (var snapshot in entry.Values)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
INSERT INTO DeleteHistoryValues(HistoryId, ValueName, ValueKind, ValueData)
VALUES(@historyId, @valueName, @valueKind, @valueData);";
                            command.Parameters.AddWithValue("@historyId", id);
                            command.Parameters.AddWithValue("@valueName", snapshot.Name ?? string.Empty);
                            command.Parameters.AddWithValue("@valueKind", snapshot.Kind);
                            command.Parameters.AddWithValue("@valueData", (object)snapshot.Data ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
                entry.Id = id;
            }
        }

        public static List<HistoryEntry> GetHistory()
        {
            var entries = new List<HistoryEntry>();
            var valuesMap = new Dictionary<long, List<RegValueSnapshot>>();

            using (var connection = Open())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT Id, Root, KeyName, Operation, OldKeyName, NewKeyName, Summary, OperatedAt, IsRestored, RestoredAt
FROM DeleteHistory
ORDER BY Id DESC;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entries.Add(new HistoryEntry
                            {
                                Id = reader.GetInt64(0),
                                Root = (RootKind)reader.GetInt32(1),
                                KeyName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Operation = (HistoryOperation)reader.GetInt32(3),
                                OldKeyName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                NewKeyName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Summary = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                OperatedAt = ParseTime(reader.IsDBNull(7) ? null : reader.GetString(7)),
                                IsRestored = !reader.IsDBNull(8) && reader.GetInt32(8) != 0,
                                RestoredAt = reader.IsDBNull(9) ? (DateTime?)null : ParseTime(reader.GetString(9))
                            });
                        }
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT HistoryId, ValueName, ValueKind, ValueData FROM DeleteHistoryValues;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var historyId = reader.GetInt64(0);
                            List<RegValueSnapshot> list;
                            if (!valuesMap.TryGetValue(historyId, out list))
                            {
                                list = new List<RegValueSnapshot>();
                                valuesMap[historyId] = list;
                            }

                            list.Add(new RegValueSnapshot
                            {
                                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Kind = reader.GetInt32(2),
                                Data = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                            });
                        }
                    }
                }
            }

            foreach (var entry in entries)
            {
                List<RegValueSnapshot> list;
                entry.Values = valuesMap.TryGetValue(entry.Id, out list)
                    ? list
                    : new List<RegValueSnapshot>();
            }

            return entries;
        }

        public static void MarkRestored(long id, DateTime restoredAt)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE DeleteHistory SET IsRestored = 1, RestoredAt = @time WHERE Id = @id;";
                command.Parameters.AddWithValue("@time", FormatTime(restoredAt));
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }

        public static void DeleteHistory(IEnumerable<long> ids)
        {
            var idList = new List<long>(ids);
            if (idList.Count == 0)
            {
                return;
            }

            using (var connection = Open())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var id in idList)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM DeleteHistoryValues WHERE HistoryId = @id;";
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM DeleteHistory WHERE Id = @id;";
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        #endregion

        public static string FormatTime(DateTime time)
        {
            return time.ToUniversalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);
        }

        public static DateTime ParseTime(string text)
        {
            DateTime parsed;
            if (DateTime.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc).ToLocalTime();
            }

            return DateTime.MinValue;
        }

        /// <summary>把时间显示为当前区域格式（界面用）。</summary>
        public static string ToDisplayTime(DateTime time)
        {
            if (time <= DateTime.MinValue)
            {
                return string.Empty;
            }

            return time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        }

        /// <summary>拼接 SQL IN 子句（参数为整数，无注入风险）。</summary>
        public static string BuildIdList(IEnumerable<long> ids)
        {
            var builder = new StringBuilder();
            foreach (var id in ids)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }
                builder.Append(id.ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }
    }
}
