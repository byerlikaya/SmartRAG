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
        private const int MinTextLengthForOcrFallback = 50; // If extracted text is less than this, use OCR

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
                    var correctedText = _imageParserService?.CorrectCurrencySymbols(text, language) ?? text;
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
                    var correctedText = _imageParserService?.CorrectCurrencySymbols(text, language) ?? text;
                    textBuilder.AppendLine(correctedText);

                    if (hasEncodingIssues)
                    {
                        _logger.LogDebug("PDF page {PageNumber} is text-based with encoding issues, using extracted text (length: {Length}). OCR not available for text-based PDFs.", i, text?.Length ?? 0);
                    }
                    else if (!textIsSubstantial)
                    {
                        _logger.LogDebug("PDF page {PageNumber} is text-based with insufficient text (length: {Length}), using extracted text", i, text?.Length ?? 0);
                    }
                }

                if (shouldUseOcr && _imageParserService != null)
                {
                    try
                    {
                        var pageImageStream = await RenderPdfPageAsImageAsync(page, pdfBytes);
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
                                    var correctedText = _imageParserService?.CorrectCurrencySymbols(text, language) ?? text;
                                    textBuilder.AppendLine(correctedText);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("PDF page {PageNumber} was expected to have embedded images but none were found, using extracted text", i);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var correctedText = _imageParserService?.CorrectCurrencySymbols(text, language) ?? text;
                                textBuilder.AppendLine(correctedText);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text via OCR for PDF page {PageNumber}, using extracted text fallback", i);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var correctedText = _imageParserService?.CorrectCurrencySymbols(text, language) ?? text;
                            textBuilder.AppendLine(correctedText);
                        }
                    }
                }
                else if (shouldUseOcr && _imageParserService == null)
                {
                    _logger.LogWarning("OCR is needed for PDF page {PageNumber} but ImageParserService is not available. Enable image parsing in configuration to use OCR.", i);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        textBuilder.AppendLine(text);
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

            // Generic pattern detection for encoding issues across all languages:
            // 1. Words without spaces (e.g., "wordAWord" instead of "word A Word")
            // 2. Missing special characters (non-ASCII characters that should be present)
            // 3. Broken word patterns (consonant clusters that suggest missing vowels/special chars)

            // Pattern 1: Check for words that are missing spaces (generic for all languages)
            // Look for lowercase letter followed immediately by uppercase letter (without space)
            // This indicates words that should be separated (e.g., "tenAyrlma" → "ten Ayrılma")
            // However, we need to be careful not to flag valid patterns like "iPhone", "McDonald"
            var hasBrokenSpacing = false;
            var brokenSpacingCount = 0; // Count occurrences to avoid false positives - declared here for use in Pattern 4
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
                            // Only flag if we see multiple occurrences (suggests systematic encoding issue)
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
            // However, single-language text is valid, so we need to be careful not to flag it
            var nonAsciiCharCount = text.Count(c => c > 127); // Non-ASCII characters
            var totalCharCount = text.Count(char.IsLetter);

            // Only flag as encoding issue if:
            // 1. Text is substantial (>200 letters) - short text might be single-language only (increased from 150)
            // 2. Has broken spacing (suggests encoding corruption, not just single-language text)
            // 3. Very low non-ASCII ratio (<1.5%) combined with broken spacing (lowered from 2% to 1.5%)
            // This avoids false positives for single-language documents and isolated word issues
            // Made more conservative to reduce false positives for text-based PDFs with minor encoding quirks
            var hasFewSpecialChars = totalCharCount > 200 &&
                                     nonAsciiCharCount < totalCharCount * 0.015 &&
                                     hasBrokenSpacing;

            if (hasFewSpecialChars)
            {
                _logger.LogDebug("Detected very few non-ASCII characters with broken words: {NonAscii}/{Total} = {Ratio:P2} (threshold: 1.5%)",
                    nonAsciiCharCount, totalCharCount, (double)nonAsciiCharCount / totalCharCount);
            }

            var hasIssue = hasBrokenSpacing || hasUnusualConsonantClusters || hasFewSpecialChars || hasBrokenWordPatterns;

            if (hasIssue)
            {
                _logger.LogDebug("Encoding issue detected - BrokenSpacing: {BrokenSpacing}, ConsonantClusters: {Clusters}, FewSpecialChars: {FewChars}, BrokenWords: {BrokenWords}",
                    hasBrokenSpacing, hasUnusualConsonantClusters, hasFewSpecialChars, hasBrokenWordPatterns);
            }

            return hasIssue;
        }

        /// <summary>
        /// Renders a PDF page as an image stream for OCR processing
        /// Extracts embedded images from scanned PDFs (image-based PDFs)
        /// For text-based PDFs, this method cannot render the page to an image
        /// because the current libraries (iText7, SkiaSharp) don't support PDF-to-bitmap rendering
        /// </summary>
        private async Task<Stream> RenderPdfPageAsImageAsync(PdfPage page, byte[] pdfBytes = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var embeddedImageResult = RenderPdfPageUsingEmbeddedImages(page);
                    if (embeddedImageResult != null)
                    {
                        return embeddedImageResult;
                    }

                    _logger.LogDebug("No embedded images found in PDF page - text-based PDF cannot be rendered to image for OCR with current libraries");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract embedded images from PDF page for OCR");
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
                // Step 1: Check for common encoding issues (mis-encoded characters)
                // If text contains characters that look like they were mis-encoded from single-byte encodings
                // (e.g., "õ" instead of correct characters in languages using Latin-based scripts), attempt to fix by trying common encodings
                var fixedText = text;
                var replacementChar = '\uFFFD'; // Unicode replacement character
                var suspiciousChar = '\u00F5'; // 'õ' character that might indicate encoding issues
                
                // Check if text contains encoding issues
                if (text.Contains(replacementChar) || (text.Contains(suspiciousChar) && text.Count(c => c == suspiciousChar) > text.Length * 0.01))
                {
                    // Try to fix by attempting to decode from common single-byte encodings
                    // The issue: PDF text was extracted as UTF-8 but was actually encoded in a single-byte encoding
                    // Solution: Treat the text as if it was encoded in a single-byte encoding, then decode properly
                    var encodingNames = new[] { "Windows-1254", "Windows-1252", "ISO-8859-1" };
                    
                    foreach (var encodingName in encodingNames)
                    {
                        try
                        {
                            var encoding = Encoding.GetEncoding(encodingName);
                            // Get bytes of text as if it was UTF-8, then decode as the source encoding
                            // This handles cases where single-byte encoded text was misinterpreted as UTF-8
                            var utf8Bytes = Encoding.UTF8.GetBytes(text);
                            var decoded = encoding.GetString(utf8Bytes);
                            
                            // Re-encode to UTF-8 properly
                            var properBytes = encoding.GetBytes(decoded);
                            var correctedText = Encoding.UTF8.GetString(Encoding.Convert(encoding, Encoding.UTF8, properBytes));
                            
                            // Check if corrected text is better (has fewer replacement characters and suspicious chars)
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
                            // Continue to next encoding
                        }
                    }
                }

                // Step 2: Apply Unicode normalization (FormC - Canonical Composition)
                // This combines decomposed characters (e.g., "ğ" = "g" + combining dot below) into composed form
                // FormC is the most common form and works for all languages
                var normalizedText = fixedText.Normalize(NormalizationForm.FormC);

                // Step 3: If FormC didn't help, try FormD (Canonical Decomposition) then recompose
                // This handles cases where characters are in a different normalization form
                // Some PDFs might store text in decomposed form, which needs to be recomposed
                if (normalizedText != fixedText)
                {
                    // Text changed with FormC, try FormD as well for completeness
                    var decomposed = fixedText.Normalize(NormalizationForm.FormD);
                    if (decomposed != fixedText && decomposed != normalizedText)
                    {
                        // Recompose from FormD to FormC
                        normalizedText = decomposed.Normalize(NormalizationForm.FormC);
                    }
                }

                // Step 4: Remove or fix invalid Unicode characters
                // This preserves all valid Unicode characters from all alphabets while removing
                // only control characters (except common ones like newline, tab)
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
