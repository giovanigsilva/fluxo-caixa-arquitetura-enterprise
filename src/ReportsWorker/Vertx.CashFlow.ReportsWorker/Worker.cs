using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Vertx.CashFlow.BuildingBlocks;

namespace Vertx.CashFlow.ReportsWorker;

public sealed class Worker(FileCashFlowStore store, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await store.MutateAsync(state =>
            {
                var next = state.StatementExports
                    .Where(item => item.Status == "Pending")
                    .OrderBy(item => item.CreatedAt)
                    .FirstOrDefault();
                if (next is not null)
                {
                    next.Status = "Running";
                }

                return next;
            }, stoppingToken);

            if (job is not null)
            {
                await ProcessJobAsync(job, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessJobAsync(StatementExportJob job, CancellationToken cancellationToken)
    {
        try
        {
            var state = await store.ReadAsync(cancellationToken);
            var entries = state.Entries
                .Where(entry => entry.TenantId == job.TenantId
                                && entry.BusinessDate >= job.From
                                && entry.BusinessDate <= job.To
                                && (job.AccountId is null || entry.AccountId == job.AccountId))
                .OrderBy(entry => entry.BusinessDate)
                .ThenBy(entry => entry.CreatedAt)
                .ToArray();
            var exportRoot = Environment.GetEnvironmentVariable("VERTX_EXPORT_DIR")
                ?? Path.Combine(AppContext.BaseDirectory, ".runtime", "exports");
            var tenantDir = Path.Combine(exportRoot, job.TenantId);
            Directory.CreateDirectory(tenantDir);
            var fileName = Path.Combine(tenantDir, $"{job.Id}.{job.Format}");
            var mimeType = job.Format switch
            {
                "pdf" => "application/pdf",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "text/csv"
            };

            switch (job.Format)
            {
                case "pdf":
                    await File.WriteAllBytesAsync(fileName, StatementWriters.WritePdf(job, entries), cancellationToken);
                    break;
                case "xlsx":
                    await File.WriteAllBytesAsync(fileName, StatementWriters.WriteXlsx(job, entries), cancellationToken);
                    break;
                default:
                    await File.WriteAllTextAsync(fileName, StatementWriters.WriteCsv(job, entries), Encoding.UTF8, cancellationToken);
                    break;
            }

            await store.MutateAsync(state =>
            {
                var current = state.StatementExports.First(item => item.Id == job.Id && item.TenantId == job.TenantId);
                current.Status = "Completed";
                current.CompletedAt = DateTimeOffset.UtcNow;
                current.FileName = fileName;
                current.MimeType = mimeType;
                return true;
            }, cancellationToken);
            logger.LogInformation("Generated {Format} statement export {JobId}.", job.Format, job.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await store.MutateAsync(state =>
            {
                var current = state.StatementExports.First(item => item.Id == job.Id && item.TenantId == job.TenantId);
                current.Status = "Failed";
                current.FailureCode = "statements.generation_failed";
                return true;
            }, cancellationToken);
            logger.LogError(ex, "Failed to generate statement export {JobId}.", job.Id);
        }
    }
}

internal static class StatementWriters
{
    public static string WriteCsv(StatementExportJob job, IReadOnlyCollection<EntryRecord> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("tenantId;periodoInicio;periodoFim;entryId;data;tipo;valor;descricao;estornoDe");
        foreach (var entry in entries)
        {
            builder
                .Append(Csv(job.TenantId)).Append(';')
                .Append(job.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
                .Append(job.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
                .Append(Csv(entry.Id)).Append(';')
                .Append(entry.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
                .Append(Csv(entry.Type)).Append(';')
                .Append(entry.Amount).Append(';')
                .Append(Csv(entry.Description)).Append(';')
                .Append(Csv(entry.ReversesEntryId ?? string.Empty))
                .AppendLine();
        }

        return builder.ToString();
    }

    public static byte[] WriteXlsx(StatementExportJob job, IReadOnlyList<EntryRecord> entries)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            Add(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            Add(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Extrato" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            Add(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            Add(archive, "xl/worksheets/sheet1.xml", BuildWorksheet(job, entries));
        }

        return memory.ToArray();
    }

    public static byte[] WritePdf(StatementExportJob job, IReadOnlyCollection<EntryRecord> entries)
    {
        var lines = new List<string>
        {
            "Fluxo de Caixa Vertx - Extrato",
            $"Tenant: {job.TenantId}",
            $"Periodo: {job.From:yyyy-MM-dd} a {job.To:yyyy-MM-dd}",
            $"Gerado em UTC: {DateTimeOffset.UtcNow:O}",
            "Data | Tipo | Valor | Descricao"
        };
        lines.AddRange(entries.Take(45).Select(entry => $"{entry.BusinessDate:yyyy-MM-dd} | {entry.Type} | {entry.Amount} | {entry.Description}"));
        var content = "BT /F1 10 Tf 50 780 Td " + string.Join(" Tj T* ", lines.Select(line => $"({Pdf(line)})")) + " Tj ET";
        var stream = Encoding.ASCII.GetBytes(content);
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {stream.Length} >> stream\n{content}\nendstream endobj\n"
        };
        using var memory = new MemoryStream();
        using var writer = new StreamWriter(memory, Encoding.ASCII, leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        foreach (var obj in objects)
        {
            writer.Flush();
            offsets.Add(memory.Position);
            writer.Write(obj);
        }

        writer.Flush();
        var xref = memory.Position;
        writer.WriteLine($"xref\n0 {offsets.Count}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine(offset.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n ");
        }

        writer.WriteLine($"trailer << /Size {offsets.Count} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xref);
        writer.WriteLine("%%EOF");
        writer.Flush();
        return memory.ToArray();
    }

    private static string BuildWorksheet(StatementExportJob job, IReadOnlyList<EntryRecord> entries)
    {
        var builder = new StringBuilder();
        using var xml = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8 });
        xml.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        xml.WriteStartElement("sheetData");
        WriteRow(xml, 1, ["Tenant", job.TenantId, "De", job.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "Até", job.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)]);
        WriteRow(xml, 3, ["Data", "Tipo", "Valor", "Descricao", "EstornoDe"]);
        var rowIndex = 4;
        foreach (var entry in entries)
        {
            WriteRow(xml, rowIndex++, [entry.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), entry.Type, entry.Amount, entry.Description, entry.ReversesEntryId ?? string.Empty]);
        }

        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.Flush();
        return builder.ToString();
    }

    private static void WriteRow(XmlWriter xml, int rowIndex, IReadOnlyList<string> cells)
    {
        xml.WriteStartElement("row");
        xml.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));
        foreach (var cell in cells)
        {
            xml.WriteStartElement("c");
            xml.WriteAttributeString("t", "inlineStr");
            xml.WriteStartElement("is");
            xml.WriteElementString("t", Neutralize(cell));
            xml.WriteEndElement();
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }

    private static void Add(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(content);
    }

    private static string Csv(string value)
    {
        var neutralized = Neutralize(value);
        return "\"" + neutralized.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string Neutralize(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        return value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n'
            ? "'" + value
            : value;
    }

    private static string Pdf(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
