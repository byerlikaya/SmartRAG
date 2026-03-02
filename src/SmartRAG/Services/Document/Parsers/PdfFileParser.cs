using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using SkiaSharp;
using PDFtoImage;

using SmartRAG.Services.Shared;

namespace SmartRAG.Services.Document.Parsers;


public class PdfFileParser : IFileParser
{
    private static readonly string[] SupportedExtensions = { ".pdf" };
    private const string SupportedContentType = "application/pdf";
    private const int MinTextLengthForOcrFallback = 50;

    private readonly IImageParserService _imageParserService;
    private readonly ILogger<PdfFileParser> _logger;

    public PdfFileParser(IImageParserService imageParserService, ILogger<PdfFileParser> logger)
    {
        _imageParserService = imageParserService;
        _logger = logger;
    }

    public bool CanParse(string fileName, string contentType)
    {
        return SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) ||
               contentType == SupportedContentType;
    }

    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string? language = null)
    {
        try
        {
            var memoryStream = await CreateMemoryStreamCopy(fileStream);
            var bytes = memoryStream.ToArray();

            using var pdfReader = new PdfReader(new MemoryStream(bytes));
            using var pdfDocument = new PdfDocument(pdfReader);
            var textBuilder = new StringBuilder();
            await ExtractTextFromPdfPagesAsync(pdfDocument, textBuilder, language, bytes);

            var content = textBuilder.ToString();
            return new FileParserResult { Content = TextCleaningHelper.CleanContent(content) };
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogPdfParserFailedToParseDocument(_logger, fileName, ex);
            return new FileParserResult { Content = string.Empty };
        }
    }

    private static async Task<MemoryStream> CreateMemoryStreamCopy(Stream fileStream)
    {
        var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Extracts text from PDF pages using text extraction, with OCR fallback for image-based PDFs
    /// Tries multiple extraction strategies to handle different PDF encoding issues
    /// </summary>
    private async Task ExtractTextFromPdfPagesAsync(PdfDocument pdfDocument, StringBuilder textBuilder, string? language = null, byte[]? pdfBytes = null)
    {
        var pageCount = pdfDocument.GetNumberOfPages();

        for (var i = 1; i <= pageCount; i++)
        {
            var page = pdfDocument.GetPage(i);
            var locationStrategy = new LocationTextExtractionStrategy();
            var locationText = PdfTextExtractor.GetTextFromPage(page, locationStrategy);
            var simpleStrategy = new SimpleTextExtractionStrategy();
            var simpleText = PdfTextExtractor.GetTextFromPage(page, simpleStrategy);
            var rawText = string.IsNullOrWhiteSpace(locationText) ? simpleText :
                         string.IsNullOrWhiteSpace(simpleText) ? locationText :
                         locationText.Length >= simpleText.Length ? locationText : simpleText;
            var text = FixPdfTextEncoding(rawText);
            var hasEmbeddedImages = HasEmbeddedImages(page);

            var hasEncodingIssues = !string.IsNullOrWhiteSpace(text) && HasTextEncodingIssues(text);
            var textIsSubstantial = !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= MinTextLengthForOcrFallback;
            var shouldUseOcr = false;

            // Primary rule:
            // - If we already have substantial text from the PDF extraction, always trust and use it.
            //   We do NOT replace it with OCR, even if the page has embedded images or mild encoding issues.
            // - OCR is reserved for pages that effectively have no useful text (scanned/image-only PDFs),
            //   so that clean digital PDFs like policy documents are not degraded by OCR noise.
            if (textIsSubstantial)
            {
                var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                textBuilder.AppendLine(correctedText);
                ServiceLogMessages.LogPdfParserPageSubstantialText(_logger, i, text.Length, hasEncodingIssues, hasEmbeddedImages, null);
            }
            else
            {
                if (hasEmbeddedImages || hasEncodingIssues)
                {
                    // No substantial text but we either have images (scanned page)
                    // or serious encoding issues: try OCR as a fallback.
                    shouldUseOcr = true;
                    ServiceLogMessages.LogPdfParserPageNoSubstantialTextOcr(_logger, i, text?.Length ?? 0, hasEncodingIssues, hasEmbeddedImages, null);
                }
                else if (!string.IsNullOrWhiteSpace(text))
                {
                    // Some text exists but below the "substantial" threshold; still use it rather
                    // than risking worse OCR output.
                    var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                    textBuilder.AppendLine(correctedText);
                    ServiceLogMessages.LogPdfParserPageLimitedText(_logger, i, text.Length, null);
                }
            }

            if (!shouldUseOcr)
                continue;

            try
            {
                var pageImageStream = await RenderPdfPageAsImageAsync(page, pdfBytes, i - 1);
                if (pageImageStream != null)
                {
                    var ocrText = await _imageParserService.ExtractTextFromImageAsync(pageImageStream, language);
                    if (!string.IsNullOrWhiteSpace(ocrText))
                    {
                        textBuilder.AppendLine(ocrText);
                        ServiceLogMessages.LogPdfParserUsedOcrForPage(_logger, i, ocrText.Length, null);
                    }
                    else
                    {
                        ServiceLogMessages.LogPdfParserOcrFailedEmbeddedImage(_logger, i, null);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                            textBuilder.AppendLine(correctedText);
                        }
                    }
                }
                else
                {
                    ServiceLogMessages.LogPdfParserPageExpectedEmbeddedImagesNoneFound(_logger, i, null);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                        textBuilder.AppendLine(correctedText);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogPdfParserFailedToExtractTextViaOcrForPage(_logger, i, ex);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                    textBuilder.AppendLine(correctedText);
                }
            }
        }
    }

    /// <summary>
    /// Checks if PDF page has embedded images (indicates scanned PDF)
    /// </summary>
    private bool HasEmbeddedImages(PdfPage page)
    {
        try
        {
            var resources = page.GetResources();

            var xObjects = resources?.GetResource(PdfName.XObject);

            if (xObjects is not PdfDictionary)
                return false;

            var xObjectDict = (PdfDictionary)xObjects;

            foreach (var key in xObjectDict.KeySet())
            {
                var obj = xObjectDict.Get(key);
                if (obj is not PdfStream stream)
                    continue;
                var subtype = stream.GetAsName(PdfName.Subtype);

                if (subtype != null && subtype.GetValue() == PdfName.Image.GetValue())
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogPdfParserFailedToCheckEmbeddedImages(_logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Detects if extracted text has encoding issues (missing special characters, broken words)
    /// Generic approach that works for all languages with special characters
    /// </summary>
    private bool HasTextEncodingIssues(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var hasBrokenSpacing = false;
        var brokenSpacingCount = 0;
        for (var i = 0; i < text.Length - 1; i++)
        {
            if (!char.IsLower(text[i]) || !char.IsUpper(text[i + 1]))
                continue;

            var before = i > 0 ? text[i - 1] : ' ';
            var after = i + 2 < text.Length ? text[i + 2] : ' ';

            if (!char.IsLetter(before) || !char.IsLetter(after))
                continue;
            var isAtWordStart = i == 0 || !char.IsLetter(text[i - 1]);
            if (isAtWordStart)
                continue;
            brokenSpacingCount++;
            if (brokenSpacingCount < 2)
                continue;

            hasBrokenSpacing = true;
            ServiceLogMessages.LogPdfParserDetectedBrokenSpacing(_logger, i, before.ToString(), text[i].ToString(), (text[i + 1]).ToString(), after.ToString(), brokenSpacingCount, null);
            break;
        }

        var hasUnusualConsonantClusters = false;
        const string asciiConsonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
        var consecutiveConsonants = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (asciiConsonants.Contains(c))
            {
                consecutiveConsonants++;
                if (consecutiveConsonants < 4)
                    continue;
                var before = i - consecutiveConsonants >= 0 ? text[i - consecutiveConsonants] : ' ';
                var after = i + 1 < text.Length ? text[i + 1] : ' ';
                if (!char.IsLetter(before) || !char.IsLetter(after))
                    continue;
                hasUnusualConsonantClusters = true;
                ServiceLogMessages.LogPdfParserDetectedConsonantCluster(_logger, consecutiveConsonants, i, null);
                break;
            }

            consecutiveConsonants = 0;
        }

        const bool hasBrokenWordPatterns = false;
        var nonAsciiCharCount = text.Count(c => c > 127);
        var totalCharCount = text.Count(char.IsLetter);

        var hasFewSpecialChars = totalCharCount > 200 &&
                                 nonAsciiCharCount < totalCharCount * 0.015 &&
                                 hasBrokenSpacing;

        if (hasFewSpecialChars)
        {
            ServiceLogMessages.LogPdfParserDetectedFewNonAscii(_logger, nonAsciiCharCount, totalCharCount, (double)nonAsciiCharCount / totalCharCount, null);
        }

        // Check for suspicious character frequency (common in encoding mismatches)
        // Certain Unicode characters (U+00A0-U+00FF range) are valid in specific alphabets
        // but when they appear frequently in otherwise Latin-script text, it indicates encoding issues
        var hasSuspiciousCharacters = false;
        var suspiciousChars = new[] { '\u00F5', '\u00A9', '\u00AA', '\u00BA', '\u00B5', '\u00B1', '\u00A7' };
        var suspiciousCharCount = text.Count(c => suspiciousChars.Contains(c));

        if (totalCharCount > 100 && suspiciousCharCount >= 3)
        {
            var ratio = (double)suspiciousCharCount / totalCharCount;
            if (ratio > 0.01) // >1% suspicious characters
            {
                hasSuspiciousCharacters = true;
                ServiceLogMessages.LogPdfParserDetectedSuspiciousChars(_logger, suspiciousCharCount, totalCharCount, ratio, null);
            }
        }

        var hasIssue = hasBrokenSpacing || hasUnusualConsonantClusters || hasFewSpecialChars || hasBrokenWordPatterns || hasSuspiciousCharacters;

        if (hasIssue)
        {
            ServiceLogMessages.LogPdfParserEncodingIssueDetected(_logger, hasBrokenSpacing, hasUnusualConsonantClusters, hasFewSpecialChars, hasBrokenWordPatterns, hasSuspiciousCharacters, null);
        }

        return hasIssue;
    }

    /// <summary>
    /// Renders a PDF page as an image stream for OCR processing
    /// First tries to extract embedded images from scanned PDFs (image-based PDFs)
    /// If no embedded images found, uses PDFium to render text-based PDF pages to bitmap
    /// </summary>
    private async Task<Stream> RenderPdfPageAsImageAsync(PdfPage page, byte[] pdfBytes = null, int pageIndex = 0)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Try embedded images first (for scanned PDFs)
                var embeddedImageResult = RenderPdfPageUsingEmbeddedImages(page);

                if (embeddedImageResult != null)
                {
                    ServiceLogMessages.LogPdfParserExtractedEmbeddedImageForOcr(_logger, pageIndex + 1, null);
                    return embeddedImageResult;
                }

                // Try PDF page rendering for text-based PDFs (if pdfBytes available)
                if (pdfBytes is { Length: > 0 })
                {
                    var renderedResult = RenderTextBasedPdfPageToImage(pdfBytes, pageIndex);
                    if (renderedResult != null)
                    {
                        ServiceLogMessages.LogPdfParserRenderedTextBasedPageToBitmap(_logger, pageIndex + 1, null);
                        return renderedResult;
                    }
                }

                ServiceLogMessages.LogPdfParserNoEmbeddedImagesPageRenderingUnavailable(_logger, pageIndex + 1, null);
                return null;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogPdfParserFailedToRenderPageToImageForOcr(_logger, pageIndex + 1, ex);
                return null;
            }
        });
    }

    /// <summary>
    /// Renders PDF page using embedded images (for scanned PDFs)
    /// </summary>
    private Stream? RenderPdfPageUsingEmbeddedImages(PdfPage page)
    {
        try
        {
            var pageSize = page.GetPageSize();
            var width = (float)pageSize.GetWidth();
            var height = (float)pageSize.GetHeight();
            const float scale = 2.0f;
            var scaledWidth = (int)(width * scale);
            var scaledHeight = (int)(height * scale);

            using var bitmap = new SKBitmap(scaledWidth, scaledHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            var imagesExtracted = ExtractImagesFromPdfPage(page, canvas, scale);

            if (!imagesExtracted)
            {
                return null;
            }

            var image = SKImage.FromBitmap(bitmap);
            var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
            var pngStream = new MemoryStream(pngData.ToArray())
            {
                Position = 0
            };

            return pngStream;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogPdfParserFailedToRenderPageUsingEmbeddedImages(_logger, ex);
            return null;
        }
    }

    /// <summary>
    /// Extracts images from PDF page and renders them to canvas
    /// </summary>
    private bool ExtractImagesFromPdfPage(PdfPage page, SKCanvas canvas, float scale)
    {
        try
        {
            var resources = page.GetResources();

            var xObjects = resources?.GetResource(iText.Kernel.Pdf.PdfName.XObject);
            if (xObjects is not PdfDictionary) return false;

            var xObjectDict = (PdfDictionary)xObjects;
            var imageFound = false;

            foreach (var key in xObjectDict.KeySet())
            {
                var obj = xObjectDict.Get(key);
                if (obj is not PdfStream stream)
                    continue;
                var subtype = stream.GetAsName(PdfName.Subtype);

                if (subtype == null || subtype.GetValue() != PdfName.Image.GetValue())
                    continue;
                try
                {
                    var pdfImage = new PdfImageXObject((PdfStream)stream);
                    var imageBytes = pdfImage.GetImageBytes();

                    if (imageBytes is { Length: > 0 })
                    {
                        using var imageStream = new MemoryStream(imageBytes);
                        using var skImage = SKImage.FromEncodedData(imageStream);
                        if (skImage != null)
                        {
                            var imageWidth = pdfImage.GetWidth();
                            var imageHeight = pdfImage.GetHeight();
                            var pageSize = page.GetPageSize();
                            var pageWidth = pageSize.GetWidth();
                            var pageHeight = pageSize.GetHeight();
                            var scaleX = (pageWidth * scale) / imageWidth;
                            var scaleY = (pageHeight * scale) / imageHeight;
                            var finalScale = Math.Min(scaleX, scaleY);
                            var destWidth = imageWidth * finalScale;
                            var destHeight = imageHeight * finalScale;
                            var x = (pageWidth * scale - destWidth) / 2;
                            var y = (pageHeight * scale - destHeight) / 2;

                            var destRect = new SKRect(x, y, x + destWidth, y + destHeight);
                            canvas.DrawImage(skImage, destRect);
                            imageFound = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogPdfParserFailedToExtractImageFromPage(_logger, ex);
                }
            }

            return imageFound;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogPdfParserFailedToExtractImagesFromPage(_logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Renders a text-based PDF page to bitmap image (for OCR fallback)
    /// This allows OCR to be performed on text-based PDFs with encoding issues
    /// Uses PDF rendering library to convert page to bitmap
    /// </summary>
    private Stream? RenderTextBasedPdfPageToImage(byte[] pdfBytes, int pageIndex)
    {
        try
        {
            // Render PDF page to bitmap
            using var originalBitmap = Conversion.ToImage(pdfBytes, page: pageIndex);

            // Upscale for better OCR accuracy (2x scale = ~600 DPI equivalent)
            // Higher resolution helps OCR distinguish similar characters (|/1, O/0, etc.)
            const float upscaleFactorForOcr = 2.0f;
            var targetWidth = (int)(originalBitmap.Width * upscaleFactorForOcr);
            var targetHeight = (int)(originalBitmap.Height * upscaleFactorForOcr);

            using var scaledBitmap = originalBitmap.Resize(
                new SKImageInfo(targetWidth, targetHeight),
                SKSamplingOptions.Default);

            if (scaledBitmap == null)
            {
                ServiceLogMessages.LogPdfParserFailedToUpscaleBitmap(_logger, pageIndex, null);

                // Fallback to original bitmap
                using var image = SKImage.FromBitmap(originalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                var stream = new MemoryStream(data.ToArray());
                stream.Position = 0;

                ServiceLogMessages.LogPdfParserSuccessfullyRenderedPageToBitmap(_logger, pageIndex, originalBitmap.Width, originalBitmap.Height, null);

                return stream;
            }

            // Encode upscaled bitmap to PNG stream for OCR processing
            using var scaledImage = SKImage.FromBitmap(scaledBitmap);
            using var scaledData = scaledImage.Encode(SKEncodedImageFormat.Png, 100);
            var finalStream = new MemoryStream(scaledData.ToArray());
            finalStream.Position = 0;

            ServiceLogMessages.LogPdfParserSuccessfullyRenderedAndUpscaled(_logger, pageIndex, originalBitmap.Width, originalBitmap.Height, scaledBitmap.Width, scaledBitmap.Height, null);

            return finalStream;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogPdfParserFailedToRenderPageToBitmap(_logger, pageIndex, ex);
            return null;
        }
    }


    /// <summary>
    /// Fixes common PDF text encoding issues
    /// PDF text extraction often produces incorrectly encoded characters, especially for non-ASCII characters
    /// This method attempts to correct encoding issues by:
    /// 1. Attempting to fix common encoding mismatches (Windows-1252/1254/ISO-8859-1 misinterpreted as UTF-8)
    /// 2. Applying Unicode normalization (FormC - Canonical Composition)
    /// 3. Applying Unicode normalization (FormD - Canonical Decomposition) and recomposing
    /// 4. Fixing replacement characters () by attempting to decode using common encodings
    /// 5. Removing invalid Unicode characters
    ///
    /// This approach is generic and works for all alphabets and writing systems
    /// without requiring language-specific character mappings.
    /// </summary>
    private string FixPdfTextEncoding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        try
        {
            var fixedText = text;
            const char replacementChar = '\uFFFD';
            const char suspiciousChar = '\u00F5';

            var totalLetterCount = text.Count(char.IsLetter);
            var latinLikeLetterCount = text.Count(c => char.IsLetter(c) && c <= 0x024F);
            var nonLatinLetterCount = totalLetterCount - latinLikeLetterCount;

            var isLatinLikeDominant = totalLetterCount > 0 &&
                                      latinLikeLetterCount >= nonLatinLetterCount * 2;

            if (isLatinLikeDominant &&
                (text.Contains(replacementChar) ||
                 (text.Contains(suspiciousChar) && text.Count(c => c == suspiciousChar) > text.Length * 0.01)))
            {
                var encodingNames = new[] { "Windows-1254", "Windows-1252", "ISO-8859-1" };

                foreach (var encodingName in encodingNames)
                {
                    try
                    {
                        var legacyEncoding = Encoding.GetEncoding(encodingName);

                        var legacyBytes = legacyEncoding.GetBytes(text);
                        var correctedText = Encoding.UTF8.GetString(legacyBytes);

                        var originalReplacementCount = text.Count(c => c == replacementChar);
                        var correctedReplacementCount = correctedText.Count(c => c == replacementChar);
                        var originalSuspiciousCount = text.Count(c => c == suspiciousChar);
                        var correctedSuspiciousCount = correctedText.Count(c => c == suspiciousChar);

                        if ((correctedReplacementCount >= originalReplacementCount) &&
                            (correctedReplacementCount != originalReplacementCount ||
                             correctedSuspiciousCount >= originalSuspiciousCount) &&
                            (correctedReplacementCount != originalReplacementCount ||
                             correctedSuspiciousCount != originalSuspiciousCount ||
                             !(correctedText.Length > text.Length * 0.9)))
                            continue;

                        fixedText = correctedText;
                        ServiceLogMessages.LogPdfParserFixedTextEncoding(_logger, encodingName, originalReplacementCount, correctedReplacementCount, originalSuspiciousCount, correctedSuspiciousCount, null);
                        break;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            var normalizedText = fixedText.Normalize(NormalizationForm.FormC);

            if (normalizedText != fixedText)
            {
                var decomposed = fixedText.Normalize(NormalizationForm.FormD);
                if (decomposed != fixedText && decomposed != normalizedText)
                {
                    normalizedText = decomposed.Normalize(NormalizationForm.FormC);
                }
            }

            normalizedText = RemoveInvalidUnicodeCharacters(normalizedText);

            normalizedText = FixOWithTildeAsDotlessI(normalizedText);

            return normalizedText;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogPdfParserFailedToFixTextEncoding(_logger, ex);
            return text;
        }
    }

    /// <summary>
    /// Removes or fixes invalid Unicode characters that might cause issues
    /// </summary>
    private static string RemoveInvalidUnicodeCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            if (!char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Fixes PDF font encoding issues where U+0131 is incorrectly extracted as U+00F5 or U+00A9.
    /// Only applies when the text contains Latin Extended characters indicating encoding mismatch.
    /// </summary>
    private static string FixOWithTildeAsDotlessI(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        const char oWithTilde = '\u00F5';
        const char copyrightSymbol = '\u00A9';
        const char dotlessI = '\u0131';

        var hasIndicativeChars = text.Any(c => c >= '\u0100' && c <= '\u024F');

        if (!hasIndicativeChars)
            return text;

        var result = text;
        if (result.IndexOf(oWithTilde) >= 0)
            result = result.Replace(oWithTilde, dotlessI);

        if (result.IndexOf(copyrightSymbol) >= 0)
            result = Regex.Replace(result, @"(\p{L})" + copyrightSymbol + @"(\p{L})", "$1" + dotlessI + "$2");

        return result;
    }

}

