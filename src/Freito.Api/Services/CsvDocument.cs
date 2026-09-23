using System.Text;

namespace Freito.Api.Services;

public sealed record CsvRow(int Number, IReadOnlyDictionary<string, string> Values);

public sealed record CsvDocument(IReadOnlyList<CsvRow> Rows)
{
    public const int MaxRows = 5_000;

    public string? Get(CsvRow row, string column) => row.Values.TryGetValue(column, out var value) ? value : null;

    public static (CsvDocument? Document, string? Error) Parse(string content, IReadOnlyCollection<string> requiredColumns)
    {
        using var reader = new StringReader(content.TrimStart('\uFEFF'));
        string? headerLine = null;
        var lineNumber = 0;
        while ((headerLine = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (!string.IsNullOrWhiteSpace(headerLine))
            {
                break;
            }
        }

        if (headerLine is null)
        {
            return (null, "The CSV file is empty.");
        }

        if (!TryParseLine(headerLine, out var headerFields, out var headerError))
        {
            return (null, $"Header row is invalid: {headerError}");
        }

        var headers = headerFields.Select(x => x.Trim()).ToArray();
        if (headers.Any(string.IsNullOrWhiteSpace))
        {
            return (null, "CSV headers cannot be blank.");
        }

        var headerSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (headers.Any(header => !headerSet.Add(header)))
        {
            return (null, "CSV headers must be unique.");
        }

        var missing = requiredColumns.Where(required => !headerSet.Contains(required)).ToArray();
        if (missing.Length > 0)
        {
            return (null, $"Missing required CSV columns: {string.Join(", ", missing)}.");
        }

        var rows = new List<CsvRow>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!TryParseLine(line, out var values, out var rowError))
            {
                return (null, $"CSV row {lineNumber} is invalid: {rowError}");
            }

            if (values.Length != headers.Length)
            {
                return (null, $"CSV row {lineNumber} has {values.Length} fields; expected {headers.Length}.");
            }

            rows.Add(new CsvRow(lineNumber, headers
                .Select((header, index) => (header, value: values[index].Trim()))
                .ToDictionary(x => x.header, x => x.value, StringComparer.OrdinalIgnoreCase)));

            if (rows.Count > MaxRows)
            {
                return (null, $"CSV files may contain at most {MaxRows} data rows.");
            }
        }

        if (rows.Count == 0)
        {
            return (null, "The CSV file has no data rows.");
        }

        return (new CsvDocument(rows), null);
    }

    private static bool TryParseLine(string line, out string[] fields, out string error)
    {
        var parsed = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var closedQuote = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (inQuotes)
            {
                if (character == '"' && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    inQuotes = false;
                    closedQuote = true;
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (closedQuote)
            {
                if (character == ',')
                {
                    parsed.Add(field.ToString());
                    field.Clear();
                    closedQuote = false;
                }
                else if (!char.IsWhiteSpace(character))
                {
                    fields = [];
                    error = "Unexpected content after a quoted field.";
                    return false;
                }

                continue;
            }

            if (character == ',' )
            {
                parsed.Add(field.ToString());
                field.Clear();
            }
            else if (character == '"')
            {
                if (field.ToString().Trim().Length != 0)
                {
                    fields = [];
                    error = "A quote appeared inside an unquoted field.";
                    return false;
                }

                field.Clear();
                inQuotes = true;
            }
            else
            {
                field.Append(character);
            }
        }

        if (inQuotes)
        {
            fields = [];
            error = "A quoted field is not closed. Fields cannot span multiple lines.";
            return false;
        }

        parsed.Add(field.ToString());
        fields = parsed.ToArray();
        error = string.Empty;
        return true;
    }
}
