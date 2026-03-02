using OfficeOpenXml;

namespace SmartRAG.Services.Document.Parsers;


public class ExcelFileParser : IFileParser
{
    private static readonly string[] SupportedExtensions = { ".xlsx", ".xls" };
    private static readonly string[] SupportedContentTypes = {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.ms-excel.sheet.macroEnabled.12"
    };

    private const string WorksheetFormat = "Worksheet: {0}";
    private const string EmptyWorksheetFormat = "Worksheet: {0} (empty)";

    public bool CanParse(string fileName, string contentType)
    {
        return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               SupportedContentTypes.Any(contentType.Contains);
    }

    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string? language = null)
    {
        try
        {
            var memoryStream = await CreateMemoryStreamCopy(fileStream);

            using var package = new ExcelPackage(memoryStream);
            var textBuilder = new StringBuilder();

            if (package.Workbook.Worksheets.Count == 0)
            {
                return new FileParserResult { Content = "Excel file contains no worksheets" };
            }

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                if (worksheet.Dimension != null)
                {
                    textBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, WorksheetFormat, worksheet.Name));

                    var rowCount = worksheet.Dimension.Rows;
                    var colCount = worksheet.Dimension.Columns;

                    var hasData = false;
                    for (var row = 1; row <= rowCount; row++)
                    {
                        var rowBuilder = new StringBuilder();
                        var rowHasData = false;

                        for (var col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            if (cellValue != null)
                            {
                                var cellText = cellValue.ToString();
                                if (!string.IsNullOrWhiteSpace(cellText))
                                {
                                    rowBuilder.Append(cellText);
                                    rowHasData = true;
                                }
                                else
                                {
                                    rowBuilder.Append(' ');
                                }
                            }
                            else
                            {
                                rowBuilder.Append(' ');
                            }

                            if (col < colCount)
                                rowBuilder.Append('\t');
                        }

                        if (!rowHasData)
                            continue;

                        textBuilder.AppendLine(rowBuilder.ToString());
                        hasData = true;
                    }

                    if (!hasData)
                    {
                        textBuilder.AppendLine("Worksheet contains no data");
                    }

                    textBuilder.AppendLine();
                }
                else
                {
                    textBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, EmptyWorksheetFormat, worksheet.Name));
                }
            }

            var content = textBuilder.ToString();
            var cleanedContent = TextCleaningHelper.CleanContent(content);

            return string.IsNullOrWhiteSpace(cleanedContent) ?
                new FileParserResult { Content = "Excel file processed but no text content extracted" } :
                new FileParserResult { Content = cleanedContent };
        }
        catch (Exception ex)
        {
            return new FileParserResult { Content = $"Error parsing Excel file: {ex.Message}" };
        }
    }

    private static async Task<MemoryStream> CreateMemoryStreamCopy(Stream fileStream)
    {
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
}

