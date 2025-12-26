using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Models;
using SmartRAG.Services.Helpers;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using PDFtoImage;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document.Parsers
{
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

        public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName, string language = null)
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
                _logger.LogError(ex, "Failed to parse PDF document: {FileName}", fileName);
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
        private async Task ExtractTextFromPdfPagesAsync(PdfDocument pdfDocument, StringBuilder textBuilder, string language = null, byte[] pdfBytes = null)
        {
            var pageCount = pdfDocument.GetNumberOfPages();

            for (int i = 1; i <= pageCount; i++)
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

                if (textIsSubstantial && !hasEncodingIssues)
                {
                    var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                    textBuilder.AppendLine(correctedText);
                    _logger.LogDebug("PDF page {PageNumber} text extraction successful, using extracted text (length: {Length})", i, text.Length);
                }
                else if (hasEmbeddedImages)
                {
                    shouldUseOcr = true;
                    if (hasEncodingIssues)
                    {
                        _logger.LogDebug("PDF page {PageNumber} has encoding issues and embedded images, using OCR (text length: {Length})", i, text?.Length ?? 0);
                    }
                    else if (!textIsSubstantial)
                    {
                        _logger.LogDebug("PDF page {PageNumber} has embedded images but insufficient text (length: {Length}), using OCR", i, text?.Length ?? 0);
                    }
                }
                else
                {
                    if (hasEncodingIssues)
                    {
                        shouldUseOcr = true;
                        _logger.LogDebug("PDF page {PageNumber} is text-based with encoding issues, attempting OCR fallback (text length: {Length})", i, text?.Length ?? 0);
                    }
                    else
                    {
                        var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                        textBuilder.AppendLine(correctedText);

                        if (hasEncodingIssues)
                        {
                            _logger.LogDebug("PDF page {PageNumber} is text-based with encoding issues, using extracted text (length: {Length})", i, text?.Length ?? 0);
                        }
                        else if (!textIsSubstantial)
                        {
                            _logger.LogDebug("PDF page {PageNumber} is text-based with insufficient text (length: {Length}), using extracted text", i, text?.Length ?? 0);
                        }
                    }
                }

                if (shouldUseOcr)
                {
                    try
                    {
                        var pageImageStream = await RenderPdfPageAsImageAsync(page, pdfBytes, i - 1);
                        if (pageImageStream != null)
                        {
                            var ocrText = await _imageParserService.ExtractTextFromImageAsync(pageImageStream, language);
                            if (!string.IsNullOrWhiteSpace(ocrText))
                            {
                                textBuilder.AppendLine(ocrText);
                                _logger.LogDebug("Used OCR for PDF page {PageNumber} from embedded image (extracted text length: {Length} chars)",
                                    i, ocrText.Length);
                            }
                            else
                            {
                                _logger.LogWarning("OCR failed to extract text from embedded image on PDF page {PageNumber}, using extracted text fallback", i);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                                    textBuilder.AppendLine(correctedText);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("PDF page {PageNumber} was expected to have embedded images but none were found, using extracted text", i);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                                textBuilder.AppendLine(correctedText);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text via OCR for PDF page {PageNumber}, using extracted text fallback", i);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var correctedText = _imageParserService.CorrectCurrencySymbols(text);
                            textBuilder.AppendLine(correctedText);
                        }
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
                if (resources == null) return false;

                var xObjects = resources.GetResource(iText.Kernel.Pdf.PdfName.XObject);
                if (xObjects == null || !(xObjects is PdfDictionary)) return false;

                var xObjectDict = (PdfDictionary)xObjects;

                foreach (var key in xObjectDict.KeySet())
                {
                    var obj = xObjectDict.Get(key);
                    if (obj is PdfStream stream)
                    {
                        var subtype = stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype);

                        if (subtype != null && subtype.GetValue() == iText.Kernel.Pdf.PdfName.Image.GetValue())
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check for embedded images in PDF page");
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
            for (int i = 0; i < text.Length - 1; i++)
            {
                if (char.IsLower(text[i]) && char.IsUpper(text[i + 1]))
                {
                    var before = i > 0 ? text[i - 1] : ' ';
                    var after = i + 2 < text.Length ? text[i + 2] : ' ';

                    if (char.IsLetter(before) && char.IsLetter(after))
                    {
                        var isAtWordStart = i == 0 || !char.IsLetter(text[i - 1]);
                        if (!isAtWordStart)
                        {
                            brokenSpacingCount++;
                            if (brokenSpacingCount >= 2)
                            {
                                hasBrokenSpacing = true;
                                _logger.LogDebug("Detected broken spacing at position {Position}: '{Before}{Char1}{Char2}{After}' (count: {Count})",
                                    i, before, text[i], text[i + 1], after, brokenSpacingCount);
                                break;
                            }
                        }
                    }
                }
            }

            var hasUnusualConsonantClusters = false;
            var asciiConsonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
            var consecutiveConsonants = 0;

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (asciiConsonants.Contains(c))
                {
                    consecutiveConsonants++;
                    if (consecutiveConsonants >= 4)
                    {
                        var before = i - consecutiveConsonants >= 0 ? text[i - consecutiveConsonants] : ' ';
                        var after = i + 1 < text.Length ? text[i + 1] : ' ';
                        if (char.IsLetter(before) && char.IsLetter(after))
                        {
                            hasUnusualConsonantClusters = true;
                            _logger.LogDebug("Detected unusual consonant cluster: {Count} consecutive ASCII consonants at position {Position}",
                                consecutiveConsonants, i);
                            break;
                        }
                    }
                }
                else
                {
                    consecutiveConsonants = 0;
                }
            }

            var hasBrokenWordPatterns = false;
            var nonAsciiCharCount = text.Count(c => c > 127);
            var totalCharCount = text.Count(char.IsLetter);

            var hasFewSpecialChars = totalCharCount > 200 &&
                                     nonAsciiCharCount < totalCharCount * 0.015 &&
                                     hasBrokenSpacing;

            if (hasFewSpecialChars)
            {
                _logger.LogDebug("Detected very few non-ASCII characters with broken words: {NonAscii}/{Total} = {Ratio:P2} (threshold: 1.5%)",
                    nonAsciiCharCount, totalCharCount, (double)nonAsciiCharCount / totalCharCount);
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
                    _logger.LogDebug("Detected high frequency of suspicious characters: {Count}/{Total} = {Ratio:P2} (threshold: 1%)",
                        suspiciousCharCount, totalCharCount, ratio);
                }
            }

            var hasIssue = hasBrokenSpacing || hasUnusualConsonantClusters || hasFewSpecialChars || hasBrokenWordPatterns || hasSuspiciousCharacters;

            if (hasIssue)
            {
                _logger.LogDebug("Encoding issue detected - BrokenSpacing: {BrokenSpacing}, ConsonantClusters: {Clusters}, FewSpecialChars: {FewChars}, BrokenWords: {BrokenWords}, SuspiciousChars: {SuspiciousChars}",
                    hasBrokenSpacing, hasUnusualConsonantClusters, hasFewSpecialChars, hasBrokenWordPatterns, hasSuspiciousCharacters);
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
                        _logger.LogDebug("Extracted embedded image from PDF page {PageIndex} for OCR", pageIndex + 1);
                        return embeddedImageResult;
                    }

                    // Try PDF page rendering for text-based PDFs (if pdfBytes available)
                    if (pdfBytes != null && pdfBytes.Length > 0)
                    {
                        var renderedResult = RenderTextBasedPdfPageToImage(pdfBytes, pageIndex);
                        if (renderedResult != null)
                        {
                            _logger.LogDebug("Rendered text-based PDF page {PageIndex} to bitmap for OCR", pageIndex + 1);
                            return renderedResult;
                        }
                    }

                    _logger.LogDebug("No embedded images found and page rendering unavailable for PDF page {PageIndex}", pageIndex + 1);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to render PDF page {PageIndex} to image for OCR", pageIndex + 1);
                    return null;
                }
            });
        }

        /// <summary>
        /// Renders PDF page using embedded images (for scanned PDFs)
        /// </summary>
        private Stream RenderPdfPageUsingEmbeddedImages(PdfPage page)
        {
            try
            {
                var pageSize = page.GetPageSize();
                var width = (float)pageSize.GetWidth();
                var height = (float)pageSize.GetHeight();
                var scale = 2.0f;
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
                _logger.LogDebug(ex, "Failed to render PDF page using embedded images");
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
                if (resources == null) return false;

                var xObjects = resources.GetResource(iText.Kernel.Pdf.PdfName.XObject);
                if (xObjects == null || !(xObjects is PdfDictionary)) return false;

                var xObjectDict = (PdfDictionary)xObjects;
                var imageFound = false;

                foreach (var key in xObjectDict.KeySet())
                {
                    var obj = xObjectDict.Get(key);
                    if (obj is PdfStream stream)
                    {
                        var subtype = stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype);

                        if (subtype != null && subtype.GetValue() == iText.Kernel.Pdf.PdfName.Image.GetValue())
                        {
                            try
                            {
                                var pdfImage = new PdfImageXObject((PdfStream)stream);
                                var imageBytes = pdfImage.GetImageBytes();

                                if (imageBytes != null && imageBytes.Length > 0)
                                {
                                    using var imageStream = new MemoryStream(imageBytes);
                                    using var skImage = SKImage.FromEncodedData(imageStream);
                                    if (skImage != null)
                                    {
                                        var imageWidth = pdfImage.GetWidth();
                                        var imageHeight = pdfImage.GetHeight();
                                        var pageSize = page.GetPageSize();
                                        var pageWidth = (float)pageSize.GetWidth();
                                        var pageHeight = (float)pageSize.GetHeight();
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
                                _logger.LogDebug(ex, "Failed to extract image from PDF page");
                            }
                        }
                    }
                }

                return imageFound;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract images from PDF page");
                return false;
            }
        }

        /// <summary>
        /// Renders a text-based PDF page to bitmap image (for OCR fallback)
        /// This allows OCR to be performed on text-based PDFs with encoding issues
        /// Uses PDF rendering library to convert page to bitmap
        /// </summary>
        private Stream RenderTextBasedPdfPageToImage(byte[] pdfBytes, int pageIndex)
        {
            try
            {
                // Render PDF page to bitmap
                using var originalBitmap = Conversion.ToImage(pdfBytes, page: pageIndex);
                
                if (originalBitmap == null)
                {
                    _logger.LogDebug("Failed to render PDF page {PageIndex} to bitmap", pageIndex);
                    return null;
                }

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
                    _logger.LogDebug("Failed to upscale bitmap for PDF page {PageIndex}, using original resolution", pageIndex);
                    
                    // Fallback to original bitmap
                    using var image = SKImage.FromBitmap(originalBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    var stream = new MemoryStream(data.ToArray());
                    stream.Position = 0;
                    
                    _logger.LogDebug("Successfully rendered PDF page {PageIndex} to bitmap ({Width}x{Height})", 
                        pageIndex, originalBitmap.Width, originalBitmap.Height);
                    
                    return stream;
                }

                // Encode upscaled bitmap to PNG stream for OCR processing
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var scaledData = scaledImage.Encode(SKEncodedImageFormat.Png, 100);
                var finalStream = new MemoryStream(scaledData.ToArray());
                finalStream.Position = 0;

                _logger.LogDebug("Successfully rendered and upscaled PDF page {PageIndex} to bitmap ({OriginalWidth}x{OriginalHeight} → {ScaledWidth}x{ScaledHeight})", 
                    pageIndex, originalBitmap.Width, originalBitmap.Height, scaledBitmap.Width, scaledBitmap.Height);
                
                return finalStream;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to render PDF page {PageIndex} to bitmap", pageIndex);
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
        /// This approach is generic and works for all alphabets (Latin, Cyrillic, Arabic, Chinese, Japanese, Korean, etc.)
        /// without requiring language-specific character mappings.
        /// </summary>
        private string FixPdfTextEncoding(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                var fixedText = text;
                var replacementChar = '\uFFFD';
                var suspiciousChar = '\u00F5';

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

                            if ((correctedReplacementCount < originalReplacementCount) ||
                                (correctedReplacementCount == originalReplacementCount && correctedSuspiciousCount < originalSuspiciousCount) ||
                                (correctedReplacementCount == originalReplacementCount && correctedSuspiciousCount == originalSuspiciousCount && correctedText.Length > text.Length * 0.9))
                            {
                                fixedText = correctedText;
                                _logger.LogDebug("Fixed PDF text encoding using {Encoding}: replacement chars {Original}->{Corrected}, suspicious chars {OriginalSusp}->{CorrectedSusp}",
                                    encodingName, originalReplacementCount, correctedReplacementCount, originalSuspiciousCount, correctedSuspiciousCount);
                                break;
                            }
                        }
                        catch
                        {
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

                return normalizedText;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fix PDF text encoding, using original text");
                return text;
            }
        }

        /// <summary>
        /// Removes or fixes invalid Unicode characters that might cause issues
        /// </summary>
        private string RemoveInvalidUnicodeCharacters(string text)
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

    }
}
