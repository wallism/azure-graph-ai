using System.Text.RegularExpressions;

namespace CloudGraphAI.AI.Plugins;

internal static partial class CypherSafety
{
    private static readonly Regex ForbiddenWriteClauses = new(
        @"\b(CREATE|MERGE|SET|DELETE|DETACH|REMOVE|DROP|LOAD\s+CSV|CALL)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ReadStart = new(
        @"^\s*(MATCH|OPTIONAL\s+MATCH|WITH|UNWIND|RETURN)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExistingLimit = new(
        @"\bLIMIT\s+\d+\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string PrepareReadOnlyQuery(string query, int maxRows)
    {
        var normalized = StripCodeFence(query).Trim();
        if (normalized.EndsWith(';'))
            normalized = normalized[..^1].Trim();

        if (normalized.Contains(';'))
            throw new InvalidOperationException("Only one Cypher statement can be executed.");

        if (!ReadStart.IsMatch(normalized))
            throw new InvalidOperationException("Only read-only Cypher queries beginning with MATCH, OPTIONAL MATCH, WITH, UNWIND, or RETURN are allowed.");

        if (ForbiddenWriteClauses.IsMatch(normalized))
            throw new InvalidOperationException("The query contains a clause that is not allowed in read-only mode.");

        if (!ExistingLimit.IsMatch(normalized))
            normalized = $"{normalized}{Environment.NewLine}LIMIT {maxRows}";

        return normalized;
    }

    private static string StripCodeFence(string query)
    {
        var trimmed = query.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return query;

        var lines = trimmed.Split(Environment.NewLine, StringSplitOptions.None).ToList();
        if (lines.Count >= 2 && lines[0].StartsWith("```", StringComparison.Ordinal) && lines[^1].StartsWith("```", StringComparison.Ordinal))
            return string.Join(Environment.NewLine, lines.Skip(1).Take(lines.Count - 2));

        return query;
    }
}
