using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AcuPower.CustomizationTools.Builder.Schema
{
    /// <summary>
    /// Converts SQL CREATE TABLE statements to Acumatica-format table schema XML.
    /// </summary>
    public static class TableSchemaBuilder
    {
        /// <summary>
        /// Parses a CREATE TABLE DDL statement and returns Acumatica table schema XML.
        /// </summary>
        /// <param name="createTableSql">SQL CREATE TABLE statement.</param>
        /// <returns>Table schema XML string.</returns>
        public static string FromSqlDdl(string createTableSql)
        {
            if (string.IsNullOrWhiteSpace(createTableSql))
                throw new ArgumentException("SQL DDL cannot be null or empty.", nameof(createTableSql));

            var tableName = ExtractTableName(createTableSql);
            var columns = ExtractColumns(createTableSql);
            var primaryKey = ExtractPrimaryKey(createTableSql);

            var sb = new StringBuilder();
            sb.AppendLine($"<table name=\"{tableName}\">");

            foreach (var col in columns)
            {
                sb.Append($"  <col name=\"{col.Name}\" type=\"{col.Type}\"");
                if (col.IsNullable)
                    sb.Append(" nullable=\"true\"");
                if (!string.IsNullOrEmpty(col.DefaultValue))
                    sb.Append($" default=\"{col.DefaultValue}\"");
                sb.AppendLine(" />");
            }

            if (primaryKey != null && primaryKey.Columns.Count > 0)
            {
                sb.Append($"  <index name=\"{primaryKey.Name}\" primary=\"true\"");
                if (primaryKey.IsClustered)
                    sb.Append(" clustered=\"true\"");
                sb.AppendLine(">");
                foreach (var colName in primaryKey.Columns)
                {
                    sb.AppendLine($"    <col name=\"{colName}\" />");
                }
                sb.AppendLine("  </index>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        private static string ExtractTableName(string sql)
        {
            // Match CREATE TABLE [dbo].[TableName] or CREATE TABLE TableName or CREATE TABLE dbo.TableName
            var match = Regex.Match(sql,
                @"CREATE\s+TABLE\s+(?:\[?[\w]+\]?\.)?\[?([\w]+)\]?",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new FormatException("Could not extract table name from SQL DDL.");

            return match.Groups[1].Value;
        }

        private static List<ColumnInfo> ExtractColumns(string sql)
        {
            var columns = new List<ColumnInfo>();

            // Extract the content between the first ( and the matching )
            var bodyStart = sql.IndexOf('(');
            if (bodyStart < 0)
                throw new FormatException("Could not find column definitions in SQL DDL.");

            var body = ExtractParenBody(sql, bodyStart);

            // Split by commas that are not inside parentheses
            var parts = SplitByTopLevelComma(body);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();

                // Skip CONSTRAINT lines
                if (Regex.IsMatch(trimmed, @"^\s*CONSTRAINT\s", RegexOptions.IgnoreCase))
                    continue;
                // Skip PRIMARY KEY lines without CONSTRAINT keyword
                if (Regex.IsMatch(trimmed, @"^\s*PRIMARY\s+KEY", RegexOptions.IgnoreCase))
                    continue;
                // Skip INDEX/UNIQUE lines
                if (Regex.IsMatch(trimmed, @"^\s*(UNIQUE|INDEX)", RegexOptions.IgnoreCase))
                    continue;

                var col = ParseColumn(trimmed);
                if (col != null)
                    columns.Add(col);
            }

            return columns;
        }

        private static ColumnInfo ParseColumn(string columnDef)
        {
            // Pattern: [ColumnName] datatype[(size)] [NULL|NOT NULL] [DEFAULT ...]
            var match = Regex.Match(columnDef,
                @"^\[?([\w]+)\]?\s+" +
                @"(\w+(?:\s*\([^)]*\))?)" +
                @"(.*?)$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!match.Success)
                return null;

            var name = match.Groups[1].Value;
            var rawType = match.Groups[2].Value.Trim();
            var remainder = match.Groups[3].Value.Trim();

            var col = new ColumnInfo
            {
                Name = name,
                Type = MapSqlType(rawType),
                IsNullable = true // default to nullable
            };

            // Check NOT NULL / NULL
            if (Regex.IsMatch(remainder, @"\bNOT\s+NULL\b", RegexOptions.IgnoreCase))
                col.IsNullable = false;
            else if (Regex.IsMatch(remainder, @"\bIDENTITY\b", RegexOptions.IgnoreCase))
                col.IsNullable = false;

            // Check DEFAULT
            var defaultMatch = Regex.Match(remainder, @"\bDEFAULT\s*\(?\s*(.+?)\s*\)?\s*(?:,|$|NOT|NULL|\Z)", RegexOptions.IgnoreCase);
            if (!defaultMatch.Success)
            {
                // Try simpler pattern
                defaultMatch = Regex.Match(remainder, @"\bDEFAULT\s+(.+?)(?:\s+NOT|\s+NULL|\s*,|\s*$)", RegexOptions.IgnoreCase);
            }

            if (defaultMatch.Success)
            {
                var rawDefault = defaultMatch.Groups[1].Value.Trim().TrimEnd(',').Trim();
                col.DefaultValue = MapDefaultValue(rawDefault);
            }

            return col;
        }

        private static string MapSqlType(string sqlType)
        {
            // Normalize whitespace and brackets
            var normalized = sqlType.Trim().Replace("[", "").Replace("]", "");

            // Direct type mappings (case-insensitive comparison)
            var lower = normalized.ToLowerInvariant();

            if (lower == "int") return "int";
            if (lower == "bigint") return "bigint";
            if (lower == "smallint") return "smallint";
            if (lower == "tinyint") return "tinyint";
            if (lower == "bit") return "bit";
            if (lower == "datetime") return "datetime";
            if (lower == "datetime2") return "datetime2";
            if (lower == "date") return "date";
            if (lower == "time") return "time";
            if (lower == "uniqueidentifier") return "uniqueidentifier";
            if (lower == "timestamp") return "timestamp";
            if (lower == "float") return "float";
            if (lower == "real") return "real";
            if (lower == "money") return "money";
            if (lower == "smallmoney") return "smallmoney";
            if (lower == "text") return "text";
            if (lower == "ntext") return "ntext";
            if (lower == "image") return "image";
            if (lower == "xml") return "xml";
            if (lower.StartsWith("varbinary")) return normalized.ToLowerInvariant();

            // Types with parameters: nvarchar(N), varchar(N), decimal(P,S), numeric(P,S), char(N), nchar(N)
            var paramMatch = Regex.Match(normalized, @"^(\w+)\s*\((.+)\)$", RegexOptions.IgnoreCase);
            if (paramMatch.Success)
            {
                var baseName = paramMatch.Groups[1].Value.ToLowerInvariant();
                var param = paramMatch.Groups[2].Value.Trim();

                switch (baseName)
                {
                    case "nvarchar":
                        return $"nvarchar({param})";
                    case "varchar":
                        return $"varchar({param})";
                    case "char":
                        return $"char({param})";
                    case "nchar":
                        return $"nchar({param})";
                    case "decimal":
                        return $"decimal({param})";
                    case "numeric":
                        return $"decimal({param})";
                    case "binary":
                        return $"binary({param})";
                    case "varbinary":
                        return $"varbinary({param})";
                    case "float":
                        return $"float({param})";
                    case "datetime2":
                        return $"datetime2({param})";
                    default:
                        return normalized.ToLowerInvariant();
                }
            }

            return normalized.ToLowerInvariant();
        }

        private static string MapDefaultValue(string rawDefault)
        {
            if (string.IsNullOrWhiteSpace(rawDefault))
                return null;

            // Strip surrounding parens
            var val = rawDefault.Trim();
            while (val.StartsWith("(") && val.EndsWith(")"))
                val = val.Substring(1, val.Length - 2).Trim();

            var lower = val.ToLowerInvariant();

            // Numeric defaults
            if (lower == "0" || lower == "(0)")
                return "Zero";
            if (lower == "1" || lower == "(1)")
                return "One";

            // Function defaults
            if (lower == "newid()")
                return "NewUuid";
            if (lower == "getdate()" || lower == "getutcdate()")
                return "CurrentDateTime";

            // String defaults - return as-is stripped of quotes
            if (val.StartsWith("'") && val.EndsWith("'"))
                return val.Substring(1, val.Length - 2);

            // Empty string
            if (lower == "''")
                return "";

            return val;
        }

        private static PrimaryKeyInfo ExtractPrimaryKey(string sql)
        {
            // Match CONSTRAINT [PK_Name] PRIMARY KEY [CLUSTERED|NONCLUSTERED] (col1, col2, ...)
            var match = Regex.Match(sql,
                @"CONSTRAINT\s+\[?([\w]+)\]?\s+PRIMARY\s+KEY\s+(CLUSTERED|NONCLUSTERED)?\s*\(([^)]+)\)",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                // Try without CONSTRAINT keyword: PRIMARY KEY [CLUSTERED] (col1, col2)
                match = Regex.Match(sql,
                    @"PRIMARY\s+KEY\s+(CLUSTERED|NONCLUSTERED)?\s*\(([^)]+)\)",
                    RegexOptions.IgnoreCase);

                if (!match.Success)
                    return null;

                var isClustered2 = match.Groups[1].Success &&
                    match.Groups[1].Value.Equals("CLUSTERED", StringComparison.OrdinalIgnoreCase);
                var colList2 = match.Groups[2].Value;

                var tableName2 = ExtractTableName(sql);
                var pk2 = new PrimaryKeyInfo
                {
                    Name = tableName2 + "_PK",
                    IsClustered = isClustered2,
                    Columns = new List<string>()
                };

                foreach (var c in colList2.Split(','))
                {
                    var colName = c.Trim().Replace("[", "").Replace("]", "")
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    if (!string.IsNullOrEmpty(colName))
                        pk2.Columns.Add(colName);
                }

                return pk2;
            }

            var pkName = match.Groups[1].Value;
            var isClustered = match.Groups[2].Success &&
                match.Groups[2].Value.Equals("CLUSTERED", StringComparison.OrdinalIgnoreCase);
            var colListStr = match.Groups[3].Value;

            var pk = new PrimaryKeyInfo
            {
                Name = pkName,
                IsClustered = isClustered,
                Columns = new List<string>()
            };

            foreach (var c in colListStr.Split(','))
            {
                var colName = c.Trim().Replace("[", "").Replace("]", "")
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
                if (!string.IsNullOrEmpty(colName))
                    pk.Columns.Add(colName);
            }

            return pk;
        }

        private static string ExtractParenBody(string sql, int openParenIndex)
        {
            int depth = 0;
            int start = openParenIndex;
            for (int i = openParenIndex; i < sql.Length; i++)
            {
                if (sql[i] == '(') depth++;
                else if (sql[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                        return sql.Substring(start + 1, i - start - 1);
                }
            }
            return sql.Substring(start + 1);
        }

        private static List<string> SplitByTopLevelComma(string body)
        {
            var parts = new List<string>();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < body.Length; i++)
            {
                if (body[i] == '(') depth++;
                else if (body[i] == ')') depth--;
                else if (body[i] == ',' && depth == 0)
                {
                    parts.Add(body.Substring(start, i - start));
                    start = i + 1;
                }
            }

            if (start < body.Length)
                parts.Add(body.Substring(start));

            return parts;
        }

        private class ColumnInfo
        {
            public string Name;
            public string Type;
            public bool IsNullable;
            public string DefaultValue;
        }

        private class PrimaryKeyInfo
        {
            public string Name;
            public bool IsClustered;
            public List<string> Columns;
        }
    }
}
