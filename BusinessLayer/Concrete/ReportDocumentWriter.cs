using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace BusinessLayer.Concrete;

internal sealed record ReportDocument(string Title, string[] Columns, List<object?[]> Rows, string Filters, CultureInfo Culture);

internal static class ReportDocumentWriter
{
    private static readonly object FontLock = new();
    public static byte[] BuildExcel(ReportDocument report)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = report.Title;
        workbook.Properties.Author = "Modern Blog";
        var sheet = workbook.Worksheets.Add(report.Culture.TwoLetterISOLanguageName == "en" ? "Report" : "Rapor");
        var columns = report.Columns.Length;
        sheet.Range(1, 1, 1, columns).Merge().Value = report.Title;
        sheet.Range(1, 1, 1, columns).Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.FromHtml("#17365D"));
        sheet.Row(1).Height = 30;
        sheet.Range(2, 1, 2, columns).Merge().Value = report.Filters;
        sheet.Range(2, 1, 2, columns).Style.Font.SetFontColor(XLColor.FromHtml("#526174"));
        sheet.Row(2).Height = 25;
        for (var column = 0; column < columns; column++) sheet.Cell(4, column + 1).Value = report.Columns[column];
        for (var row = 0; row < report.Rows.Count; row++)
        for (var column = 0; column < columns; column++)
        {
            var cell = sheet.Cell(row + 5, column + 1);
            switch (report.Rows[row][column])
            {
                case null: break;
                case DateTime date: cell.Value = date; cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm"; break;
                case int number: cell.Value = number; cell.Style.NumberFormat.Format = "#,##0"; break;
                case long number: cell.Value = number; cell.Style.NumberFormat.Format = "#,##0"; break;
                default:
                    // Assign a text value, never FormulaA1: submitted strings cannot become Excel formulas.
                    var value = Convert.ToString(report.Rows[row][column], report.Culture) ?? string.Empty;
                    cell.Value = value;
                    if (value.Length > 0 && "=+-@".Contains(value[0])) cell.Style.IncludeQuotePrefix = true;
                    break;
            }
        }
        var table = sheet.Range(4, 1, Math.Max(5, report.Rows.Count + 4), columns).CreateTable("ReportData");
        table.Theme = XLTableTheme.TableStyleMedium2;
        table.ShowAutoFilter = true;
        table.ShowRowStripes = true;
        sheet.SheetView.FreezeRows(4);
        sheet.Columns(1, columns).AdjustToContents(4, Math.Max(5, report.Rows.Count + 4));
        foreach (var column in sheet.Columns(1, columns)) column.Width = Math.Clamp(column.Width + 2, 12, 60);
        sheet.Range(4, 1, Math.Max(5, report.Rows.Count + 4), columns).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        sheet.Range(4, 1, Math.Max(5, report.Rows.Count + 4), columns).Style.Alignment.WrapText = true;
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(4, 4);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public static byte[] BuildPdf(ReportDocument report)
    {
        EnsureFonts();
        var document = new Document();
        document.Info.Title = report.Title;
        document.Info.Author = "Modern Blog";
        document.Styles[StyleNames.Normal]!.Font.Name = "Noto Sans";
        document.Styles[StyleNames.Normal]!.Font.Size = 8;
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.2);
        var title = section.AddParagraph(report.Title);
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Color.FromRgb(23, 54, 93);
        title.Format.SpaceAfter = Unit.FromCentimeter(0.25);
        var filters = section.AddParagraph(report.Filters);
        filters.Format.Font.Color = Color.FromRgb(82, 97, 116);
        filters.Format.SpaceAfter = Unit.FromCentimeter(0.4);
        var table = section.AddTable();
        table.Borders.Width = 0.4;
        table.Borders.Color = Color.FromRgb(215, 222, 230);
        table.Format.Font.Size = 8;
        table.TopPadding = Unit.FromMillimeter(1.5);
        table.BottomPadding = Unit.FromMillimeter(1.5);
        var widths = ColumnWeights(report.Columns.Length);
        foreach (var weight in widths) table.AddColumn(Unit.FromCentimeter(27.3 * weight / widths.Sum()));
        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Colors.White;
        header.Shading.Color = Color.FromRgb(23, 54, 93);
        for (var i = 0; i < report.Columns.Length; i++) header.Cells[i].AddParagraph(report.Columns[i]);
        for (var i = 0; i < report.Rows.Count; i++)
        {
            var row = table.AddRow();
            if (i % 2 == 1) row.Shading.Color = Color.FromRgb(242, 246, 250);
            for (var j = 0; j < report.Columns.Length; j++)
            {
                var value = report.Rows[i][j] is DateTime date ? date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    : Convert.ToString(report.Rows[i][j], report.Culture) ?? string.Empty;
                row.Cells[j].AddParagraph(BreakLongTokens(value));
            }
        }
        if (report.Rows.Count == 0)
        {
            var empty = table.AddRow();
            empty.Cells[0].MergeRight = report.Columns.Length - 1;
            empty.Cells[0].AddParagraph(report.Culture.TwoLetterISOLanguageName == "en" ? "No matching records." : "Filtreye uygun kayıt bulunamadı.");
        }
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.AddText(report.Culture.TwoLetterISOLanguageName == "en" ? "Page " : "Sayfa ");
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        renderer.PdfDocument.Save(output, false);
        return output.ToArray();
    }

    // Variable widths reserve space for names, titles and dates rather than equally sizing every cell.
    private static double[] ColumnWeights(int columns) => columns switch
    {
        6 => [0.8, 3, 4.5, 5.5, 2.7, 3.4],
        7 => [0.8, 3, 4.5, 1.5, 3.4, 1.6, 1.6],
        8 => [0.8, 5.5, 1.3, 3, 1.4, 1.4, 2.2, 3.2],
        _ => Enumerable.Repeat(1d, columns).ToArray()
    };
    private static string BreakLongTokens(string value) => Regex.Replace(value, @"\S{28,}", match =>
        string.Join("\n", Enumerable.Range(0, (match.Length + 26) / 27).Select(i => match.Value.Substring(i * 27, Math.Min(27, match.Length - i * 27)))));

    private static void EnsureFonts()
    {
        lock (FontLock)
        {
            GlobalFontSettings.FontResolver ??= new EmbeddedReportFontResolver();
        }
    }

    private sealed class EmbeddedReportFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
            => new(bold ? "NotoSans-Bold" : "NotoSans-Regular", false, italic);
        public byte[]? GetFont(string faceName)
        {
            using var source = typeof(ReportService).Assembly.GetManifestResourceStream($"BusinessLayer.Assets.Fonts.{faceName}.ttf")
                ?? throw new InvalidOperationException("The embedded report font is missing.");
            using var bytes = new MemoryStream();
            source.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
