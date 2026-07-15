using ApexTracker.Models;
using ClosedXML.Excel;

namespace ApexTracker.Data;

public sealed class MatchExcelExporter
{
    public Task ExportAsync(
        IEnumerable<MatchInfo> matches,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var exportRows = matches.OrderByDescending(match => match.StartTime).ToList();

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Matches");
            var headers = new[] { "Start Time", "End Time", "Map", "Legend", "Kills", "Assists", "Damage", "Rank", "Duration" };

            for (var column = 0; column < headers.Length; column++)
            {
                worksheet.Cell(1, column + 1).Value = headers[column];
            }

            for (var row = 0; row < exportRows.Count; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var match = exportRows[row];
                var excelRow = row + 2;
                worksheet.Cell(excelRow, 1).Value = match.StartTime;
                worksheet.Cell(excelRow, 2).Value = match.EndTime;
                worksheet.Cell(excelRow, 3).Value = match.Map;
                worksheet.Cell(excelRow, 4).Value = match.Legend;
                worksheet.Cell(excelRow, 5).Value = match.Kills;
                worksheet.Cell(excelRow, 6).Value = match.Assists;
                worksheet.Cell(excelRow, 7).Value = match.Damage;
                worksheet.Cell(excelRow, 8).Value = match.Rank;
                worksheet.Cell(excelRow, 9).Value = match.Duration?.ToString("hh\\:mm\\:ss") ?? string.Empty;
            }

            var headerRange = worksheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("2D6CDF");
            headerRange.Style.Font.FontColor = XLColor.White;
            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }, cancellationToken);
    }
}
