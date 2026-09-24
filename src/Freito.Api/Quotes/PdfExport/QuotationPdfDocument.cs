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
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(16);

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

            if (model.ApprovedByName is not null)
            {
                column.Item().Text($"Approved by {model.ApprovedByName} on {FormatDate(model.ApprovedAt)}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            }

            column.Item().PaddingTop(10).Text(
                "This is a draft price based on rates in effect at the time of calculation and is not a binding contract. " +
                "Final charges may vary based on actual cargo details and destination charges at the time of shipment.")
                .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
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
