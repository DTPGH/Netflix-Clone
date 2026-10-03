using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using NetflixClone.Application.Admin.Reports;
using NetflixClone.Application.Common.Abstractions.Reporting;
namespace NetflixClone.Infrastructure.Reporting;

// Minimal SpreadsheetML workbook: explicit string cells never become Excel formulas.
public sealed class ViewingReportExcelExporter : IViewingReportExporter
{
    private const string Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    public byte[] Generate(AdminViewingReport report)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Part(zip, "[Content_Types].xml", w =>
            {
                w.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
                Empty(w, "Default", ("Extension", "rels"), ("ContentType", "application/vnd.openxmlformats-package.relationships+xml"));
                Empty(w, "Default", ("Extension", "xml"), ("ContentType", "application/xml"));
                Empty(w, "Override", ("PartName", "/xl/workbook.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"));
                Empty(w, "Override", ("PartName", "/xl/styles.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"));
                foreach (var i in new[] { 1, 2 }) Empty(w, "Override", ("PartName", $"/xl/worksheets/sheet{i}.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"));
                w.WriteEndElement();
            });
            Part(zip, "_rels/.rels", w =>
            {
                w.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                Empty(w, "Relationship", ("Id", "rId1"), ("Type", Relationships + "/officeDocument"), ("Target", "xl/workbook.xml"));
                w.WriteEndElement();
            });
            Part(zip, "xl/workbook.xml", w =>
            {
                w.WriteStartElement("workbook", Spreadsheet); w.WriteStartElement("sheets");
                for (var i = 1; i <= 2; i++)
                {
                    w.WriteStartElement("sheet"); w.WriteAttributeString("name", i == 1 ? "Summary" : "Movies");
                    w.WriteAttributeString("sheetId", i.ToString()); w.WriteAttributeString("r", "id", Relationships, $"rId{i}"); w.WriteEndElement();
                }
                w.WriteEndElement(); w.WriteEndElement();
            });
            Part(zip, "xl/_rels/workbook.xml.rels", w =>
            {
                w.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                for (var i = 1; i <= 2; i++) Empty(w, "Relationship", ("Id", $"rId{i}"), ("Type", Relationships + "/worksheet"), ("Target", $"worksheets/sheet{i}.xml"));
                Empty(w, "Relationship", ("Id", "rId3"), ("Type", Relationships + "/styles"), ("Target", "styles.xml"));
                w.WriteEndElement();
            });
            Styles(zip);
            var s = report.Summary;
            Sheet(zip, 1, new object[][] {
                ["Viewing report", "Value"], ["From UTC (inclusive)", report.FromUtc.ToString("O")],
                ["To UTC (exclusive)", report.ToUtc.ToString("O")], ["Generated at UTC", report.GeneratedAtUtc.ToString("O")],
                ["Sessions", s.Sessions], ["Qualified views (120 seconds)", s.QualifiedViews], ["Unique profiles", s.UniqueProfiles],
                ["Watched seconds", s.WatchedSeconds], ["Finished sessions", s.FinishedSessions], ["Active sessions", s.ActiveSessions],
                ["Timed-out sessions (including inferred)", s.TimedOutSessions], ["Movies with sessions", report.TotalMovies],
                ["Date attribution", "Session StartedAt; all times UTC"],
                ["Historical content", "Includes soft-deleted movies and profiles; profile identities are not exported."],
                ["Live data", "Recorded totals may change while sessions continue. Unique profiles are read in a separate query."],
                ["Demo footage", "Currently records demo playback, not proof of viewing the real movie."],
                ["Timeout", "Two minutes without checkpoint; stale unclosed sessions count as timed out."]
            }, false);
            var rows = new List<object[]> { new object[] { "Movie ID", "Title", "Deleted", "Sessions", "Qualified views", "Unique profiles", "Watched seconds", "Finished", "Active", "Timed out" } };
            rows.AddRange(report.Movies.Select(m => new object[] { m.MovieId, m.Title, m.IsDeleted ? "Yes" : "No", m.Sessions,
                m.QualifiedViews, m.UniqueProfiles, m.WatchedSeconds, m.FinishedSessions, m.ActiveSessions, m.TimedOutSessions }));
            Sheet(zip, 2, rows, true);
        }
        return output.ToArray();
    }
    private static void Part(ZipArchive zip, string name, Action<XmlWriter> write)
    {
        using var stream = zip.CreateEntry(name, CompressionLevel.Fastest).Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
        writer.WriteStartDocument(); write(writer); writer.WriteEndDocument();
    }
    private static void Empty(XmlWriter w, string name, params (string Name, string Value)[] attributes)
    {
        w.WriteStartElement(name); foreach (var a in attributes) w.WriteAttributeString(a.Name, a.Value); w.WriteEndElement();
    }
    private static void Styles(ZipArchive zip) => Part(zip, "xl/styles.xml", w =>
    {
        w.WriteStartElement("styleSheet", Spreadsheet);
        w.WriteStartElement("fonts"); w.WriteAttributeString("count", "2");
        w.WriteStartElement("font"); Empty(w, "sz", ("val", "11")); Empty(w, "name", ("val", "Calibri")); w.WriteEndElement();
        w.WriteStartElement("font"); Empty(w, "b"); Empty(w, "sz", ("val", "11")); Empty(w, "color", ("rgb", "FFFFFFFF")); Empty(w, "name", ("val", "Calibri")); w.WriteEndElement(); w.WriteEndElement();
        w.WriteStartElement("fills"); w.WriteAttributeString("count", "3");
        foreach (var pattern in new[] { "none", "gray125", "solid" })
        {
            w.WriteStartElement("fill"); w.WriteStartElement("patternFill"); w.WriteAttributeString("patternType", pattern);
            if (pattern == "solid") { Empty(w, "fgColor", ("rgb", "FFB20710")); Empty(w, "bgColor", ("indexed", "64")); }
            w.WriteEndElement(); w.WriteEndElement();
        }
        w.WriteEndElement();
        w.WriteStartElement("borders"); w.WriteAttributeString("count", "1"); w.WriteStartElement("border");
        foreach (var side in new[] { "left", "right", "top", "bottom", "diagonal" }) Empty(w, side);
        w.WriteEndElement(); w.WriteEndElement();
        w.WriteStartElement("cellStyleXfs"); w.WriteAttributeString("count", "1"); Empty(w, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0")); w.WriteEndElement();
        w.WriteStartElement("cellXfs"); w.WriteAttributeString("count", "2");
        for (var i = 0; i <= 1; i++) Empty(w, "xf", ("numFmtId", "0"), ("fontId", i.ToString()), ("fillId", i == 0 ? "0" : "2"), ("borderId", "0"), ("xfId", "0"), ("applyFill", "1"), ("applyFont", "1"));
        w.WriteEndElement(); w.WriteStartElement("cellStyles"); w.WriteAttributeString("count", "1"); Empty(w, "cellStyle", ("name", "Normal"), ("xfId", "0"), ("builtinId", "0")); w.WriteEndElement(); w.WriteEndElement();
    });
    private static void Sheet(ZipArchive zip, int id, IEnumerable<object[]> rows, bool filter) => Part(zip, $"xl/worksheets/sheet{id}.xml", w =>
    {
        w.WriteStartElement("worksheet", Spreadsheet);
        w.WriteStartElement("sheetViews"); w.WriteStartElement("sheetView"); w.WriteAttributeString("workbookViewId", "0");
        Empty(w, "pane", ("ySplit", "1"), ("topLeftCell", "A2"), ("activePane", "bottomLeft"), ("state", "frozen")); w.WriteEndElement(); w.WriteEndElement();
        w.WriteStartElement("cols"); Empty(w, "col", ("min", "1"), ("max", "1"), ("width", id == 1 ? "42" : "12"), ("customWidth", "1"));
        Empty(w, "col", ("min", "2"), ("max", "2"), ("width", id == 1 ? "95" : "45"), ("customWidth", "1"));
        if (id == 2) Empty(w, "col", ("min", "3"), ("max", "10"), ("width", "20"), ("customWidth", "1"));
        w.WriteEndElement(); w.WriteStartElement("sheetData");
        var rowNumber = 0;
        foreach (var row in rows)
        {
            rowNumber++; w.WriteStartElement("row"); w.WriteAttributeString("r", rowNumber.ToString());
            for (var i = 0; i < row.Length; i++)
            {
                w.WriteStartElement("c"); w.WriteAttributeString("r", $"{(char)('A' + i)}{rowNumber}");
                if (rowNumber == 1) w.WriteAttributeString("s", "1");
                if (row[i] is int or long)
                    w.WriteElementString("v", Convert.ToString(row[i], CultureInfo.InvariantCulture));
                else
                {
                    w.WriteAttributeString("t", "inlineStr"); w.WriteStartElement("is"); w.WriteStartElement("t");
                    w.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                    var text = Convert.ToString(row[i], CultureInfo.InvariantCulture) ?? "";
                    w.WriteString(string.Concat(text.EnumerateRunes().Where(r => r.Value is 9 or 10 or 13 || r.Value >= 32 && r.Value is not (65534 or 65535)).Select(r => r.ToString())));
                    w.WriteEndElement(); w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }
        w.WriteEndElement();
        if (filter) Empty(w, "autoFilter", ("ref", $"A1:J{rowNumber}"));
        w.WriteEndElement();
    });
}
