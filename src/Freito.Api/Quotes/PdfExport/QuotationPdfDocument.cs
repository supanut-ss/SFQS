using System.Globalization;
using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Freito.Api.Quotes.PdfExport;

/// <summary>
/// The downloadable quotation PDF (T9) — Sale attaches this to their own email/Outlook to send
/// to the customer; there's no SMTP integration in the system (confirmed with Operation,
/// technical-plan.md §7). Renders a snapshot of QuotationLine, so it never drifts from what the
/// customer was actually quoted even if rates change later.
/// </summary>
public sealed class QuotationPdfDocument(QuotationPdfModel model) : IDocument
{
    private static readonly byte[] LogoBytes = LoadEmbeddedLogo();
    private const string BrandColor = "#1D3D6B"; // primitive.color.navy.700 — tokens.json

    public DocumentMetadata GetMetadata()
    {
        var metadata = DocumentMetadata.Default;
        metadata.Title = $"Quotation {model.QuoteNo}";
        return metadata;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            // "Lato" is the only font QuestPDF bundles by default (QuestPDF.Fonts.Lato.br) —
            // any other family name throws DocumentDrawingException unless deployed alongside
            // the app or enabled via Settings.UseSystemFonts (not portable across Plesk hosts).
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.ConstantItem(140).Image(LogoBytes);
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text("QUOTATION").FontSize(18).Bold().FontColor(BrandColor);
                column.Item().Text(model.QuoteNo).FontSize(12).FontColor(Colors.Grey.Darken2);
                column.Item().Text($"Valid until {FormatDate(model.ExpiresAt)}").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(16).Column(column =>
        {
            column.Spacing(12);

            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new InfoBlockComponent("Customer", [
                    model.CustomerCompany is { Length: > 0 } company ? $"{model.CustomerName} ({company})" : model.CustomerName,
                    model.CustomerEmail,
                    model.CustomerPhone,
                ]));
                row.RelativeItem().Component(new InfoBlockComponent("Shipment", [
                    $"{model.OriginPortName} → {model.DestinationPortName}",
                    $"{model.Mode.ToUpperInvariant()} • {model.Direction}",
                    $"Incoterm: {model.IncotermCode} ({model.IncotermName})",
                    $"Ready date: {FormatDate(model.ReadyDate)}",
                ]));
            });

            // Modular section: Schedule and commercial terms (only rendered if present)
            column.Item().Element(ComposeScheduleAndTerms);

            // Modular section: Cargo dimensions (only rendered if present)
            column.Item().Element(ComposeDimensionsTable);

            column.Item().Element(ComposeLineItemsTable);

            column.Item().AlignRight().Width(220).Column(totals =>
            {
                totals.Spacing(4);
                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal");
                    r.ConstantItem(100).AlignRight().Text(FormatMoney(model.Subtotal));
                });
                if (model.DiscountAmount != 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Discount");
                        r.ConstantItem(100).AlignRight().Text($"-{FormatMoney(model.DiscountAmount)}");
                    });
                }

                totals.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(r =>
                {
                    r.RelativeItem().Text("Total").Bold().FontSize(13);
                    r.ConstantItem(100).AlignRight().Text(FormatMoney(model.FinalPrice)).Bold().FontSize(13).FontColor(BrandColor);
                });
            });

            // Modular section: Terms & conditions (only rendered if present)
            column.Item().Element(ComposeTermsAndConditions);

            if (model.ApprovedByName is not null)
            {
                column.Item().Text($"Approved by {model.ApprovedByName} on {FormatDate(model.ApprovedAt)}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            }

            column.Item().PaddingTop(4).Text(
                "This quotation is based on rates in effect at the time of calculation. " +
                "Final charges may vary based on actual cargo details and destination charges at the time of shipment.")
                .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
        });
    }

    private void ComposeScheduleAndTerms(IContainer container)
    {
        var scheduleItems = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.CarrierInfo)) scheduleItems.Add($"Carrier: {model.CarrierInfo}");
        if (!string.IsNullOrWhiteSpace(model.TransitTime)) scheduleItems.Add($"Transit time: {model.TransitTime}");
        if (!string.IsNullOrWhiteSpace(model.Frequency)) scheduleItems.Add($"Frequency: {model.Frequency}");
        if (!string.IsNullOrWhiteSpace(model.ClosingSchedule)) scheduleItems.Add($"Closing/Cut-off: {model.ClosingSchedule}");

        var commercialItems = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.PaymentTerms)) commercialItems.Add($"Payment terms: {model.PaymentTerms}");
        if (!string.IsNullOrWhiteSpace(model.InsuranceStatus)) commercialItems.Add($"Cargo insurance: {model.InsuranceStatus}");

        if (scheduleItems.Count == 0 && commercialItems.Count == 0) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
        {
            if (scheduleItems.Count > 0)
            {
                row.RelativeItem().Component(new InfoBlockComponent("Routing & Schedule", scheduleItems));
            }
            if (commercialItems.Count > 0)
            {
                row.RelativeItem().Component(new InfoBlockComponent("Commercial Terms", commercialItems));
            }
        });
    }

    private void ComposeDimensionsTable(IContainer container)
    {
        if (string.IsNullOrWhiteSpace(model.DimensionsJson)) return;

        List<QuotationDimensionItem>? items = null;
        try
        {
            items = System.Text.Json.JsonSerializer.Deserialize<List<QuotationDimensionItem>>(
                model.DimensionsJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return;
        }

        if (items is not { Count: > 0 }) return;

        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Text("Cargo Dimensions & Packing Details").FontSize(9).Bold().FontColor(BrandColor);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Qty");
                    header.Cell().Element(HeaderCell).Text("Dimensions (L × W × H cm)");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Volume (m³)");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Gross Weight (kg)");
                });

                decimal totalCbm = 0;
                decimal totalWeight = 0;
                int totalQty = 0;

                foreach (var item in items)
                {
                    var cbm = (item.LengthCm * item.WidthCm * item.HeightCm / 1_000_000m) * item.Quantity;
                    totalCbm += cbm;
                    totalWeight += item.GrossWeightKg;
                    totalQty += item.Quantity;

                    table.Cell().Element(BodyCell).Text($"{item.Quantity}");
                    table.Cell().Element(BodyCell).Text($"{item.LengthCm:N0} × {item.WidthCm:N0} × {item.HeightCm:N0} cm");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{cbm:N3} m³");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{item.GrossWeightKg:N1} kg");
                }

                table.Cell().Element(SummaryCell).Text($"Total: {totalQty}").Bold();
                table.Cell().Element(SummaryCell).Text("");
                table.Cell().Element(SummaryCell).AlignRight().Text($"{totalCbm:N3} m³").Bold();
                table.Cell().Element(SummaryCell).AlignRight().Text($"{totalWeight:N1} kg").Bold();

                static IContainer HeaderCell(IContainer c) =>
                    c.Background(Colors.Grey.Lighten2).Padding(4).DefaultTextStyle(x => x.FontSize(8).Bold());

                static IContainer BodyCell(IContainer c) =>
                    c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(x => x.FontSize(8));

                static IContainer SummaryCell(IContainer c) =>
                    c.Background(Colors.Grey.Lighten4).Padding(4).DefaultTextStyle(x => x.FontSize(8));
            });
        });
    }

    private void ComposeTermsAndConditions(IContainer container)
    {
        if (string.IsNullOrWhiteSpace(model.TermsAndConditions)) return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(col =>
        {
            col.Spacing(4);
            col.Item().Text("Terms & Conditions").FontSize(9).Bold().FontColor(BrandColor);
            var lines = model.TermsAndConditions.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                col.Item().Row(r =>
                {
                    r.ConstantItem(12).Text("•").FontSize(8).FontColor(Colors.Grey.Darken1);
                    r.RelativeItem().Text(trimmed.TrimStart('•', '-', '*').Trim()).FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            }
        });
    }

    private void ComposeLineItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Description");
                header.Cell().Element(HeaderCell).Text("Basis");
                header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
            });

            foreach (var line in model.Lines)
            {
                table.Cell().Element(BodyCell).Text(line.Description);
                table.Cell().Element(BodyCell).Text(line.Basis);
                table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.Amount, line.Currency));
            }

            return;

            static IContainer HeaderCell(IContainer c) =>
                c.Background(BrandColor).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

            static IContainer BodyCell(IContainer c) =>
                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6);
        });
    }

    private string FormatMoney(decimal amount, string? currency = null) =>
        $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {currency ?? model.QuoteCurrency}";

    // Same bug as T7's quote_no generation: an unqualified "yyyy" format string reads the
    // server's locale calendar (Thai Buddhist Era here, giving 2569 instead of 2026) —
    // InvariantCulture is required everywhere a date is formatted.
    private static string FormatDate(DateTime? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";

    private static byte[] LoadEmbeddedLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Freito.Api.Quotes.PdfExport.Assets.logo-lockup.png";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

/// <summary>Small labeled block (customer / shipment info) reused twice in the header row.</summary>
internal sealed class InfoBlockComponent(string title, IReadOnlyList<string> lines) : IComponent
{
    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
            foreach (var line in lines)
            {
                column.Item().Text(line).FontSize(10);
            }
        });
    }
}
