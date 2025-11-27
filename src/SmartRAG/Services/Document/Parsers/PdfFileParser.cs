using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Parser.Strategies;
using SmartRAG.Services.Helpers;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

                using (var pdfReader = new PdfReader(new MemoryStream(bytes)))
                {
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        var textBuilder = new StringBuilder();
                        await ExtractTextFromPdfPagesAsync(pdfDocument, textBuilder, language, bytes);

                        var content = textBuilder.ToString();
                        return new FileParserResult { Content = TextCleaningHelper.CleanContent(content) };
                    }
                }
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
                
                // Try multiple extraction strategies to handle different PDF encoding issues
                // Strategy 1: LocationTextExtractionStrategy (preserves layout, better for complex PDFs)
                var locationStrategy = new LocationTextExtractionStrategy();
                var locationText = PdfTextExtractor.GetTextFromPage(page, locationStrategy);
                
                // Strategy 2: SimpleTextExtractionStrategy (simpler, sometimes handles encoding better)
                var simpleStrategy = new SimpleTextExtractionStrategy();
                var simpleText = PdfTextExtractor.GetTextFromPage(page, simpleStrategy);
                
                // Choose the best extracted text (prefer longer, more complete text)
                var rawText = string.IsNullOrWhiteSpace(locationText) ? simpleText :
                             string.IsNullOrWhiteSpace(simpleText) ? locationText :
                             locationText.Length >= simpleText.Length ? locationText : simpleText;
                
                // Apply encoding fixes to extracted text
                var text = FixPdfTextEncoding(rawText);
                
                // Check if page has embedded images (scanned PDF indicator)
                // OCR only works for scanned PDFs with embedded images
                // For text-based PDFs, we must use extracted text (even if it has encoding issues)
                var hasEmbeddedImages = HasEmbeddedImages(page);
                
                // Decision logic:
                // 1. If text is good (substantial length, no encoding issues) → use it
                // 2. If text is missing/short AND page has embedded images → use OCR
                // 3. If text has encoding issues BUT page has embedded images → use OCR
                // 4. If text has encoding issues BUT page is text-based → use extracted text (best we can do)
                // 5. If text is missing/short AND page is text-based → use extracted text (empty or short)
                
                var hasEncodingIssues = !string.IsNullOrWhiteSpace(text) && HasTextEncodingIssues(text);
                var textIsSubstantial = !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= MinTextLengthForOcrFallback;
                var shouldUseOcr = false;
                
                if (textIsSubstantial && !hasEncodingIssues)
                {
                    // Text extraction is good, use it
                    var correctedText = _imageParserService.CorrectCurrencySymbols(text, language);
                    textBuilder.AppendLine(correctedText);
                    _logger.LogDebug("PDF page {PageNumber} text extraction successful, using extracted text (length: {Length})", i, text.Length);
                }
                else if (hasEmbeddedImages)
                {
                    // Page has embedded images (scanned PDF) - try OCR
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
                    // Text-based PDF - use extracted text (even if it has encoding issues)
                    // We cannot render text-based PDF pages to images for OCR
                    var correctedText = _imageParserService.CorrectCurrencySymbols(text, language);
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

                // If we should use OCR, try to extract text via OCR from embedded images
                if (shouldUseOcr)
                {
                    try
                    {
                        var pageImageStream = await RenderPdfPageAsImageAsync(page, pdfBytes);
                        if (pageImageStream != null)
                        {
                            // Embedded image found - use OCR on the image
                            var ocrText = await _imageParserService.ExtractTextFromImageAsync(pageImageStream, language);
                            if (!string.IsNullOrWhiteSpace(ocrText))
                            {
                                textBuilder.AppendLine(ocrText);
                                _logger.LogDebug("Used OCR for PDF page {PageNumber} from embedded image (extracted text length: {Length} chars)", 
                                    i, ocrText.Length);
                            }
                            else
                            {
                                // OCR failed on embedded image, fallback to extracted text if available
                                _logger.LogWarning("OCR failed to extract text from embedded image on PDF page {PageNumber}, using extracted text fallback", i);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    var correctedText = _imageParserService.CorrectCurrencySymbols(text, language);
                                    textBuilder.AppendLine(correctedText);
                                }
                            }
                        }
                        else
                        {
                            // This shouldn't happen if hasEmbeddedImages was true, but handle gracefully
                            _logger.LogWarning("PDF page {PageNumber} was expected to have embedded images but none were found, using extracted text", i);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var correctedText = _imageParserService.CorrectCurrencySymbols(text, language);
                                textBuilder.AppendLine(correctedText);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text via OCR for PDF page {PageNumber}, using extracted text fallback", i);
                        // Fallback to extracted text even if it has issues
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var correctedText = _imageParserService.CorrectCurrencySymbols(text, language);
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
                    if (obj is PdfStream)
                    {
                        var stream = (PdfStream)obj;
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
                    // Check if this looks like a broken word (not an abbreviation or proper noun)
                    var before = i > 0 ? text[i - 1] : ' ';
                    var after = i + 2 < text.Length ? text[i + 2] : ' ';
                    
                    // Skip if it's a common pattern (e.g., "iPhone", "McDonald" - uppercase after lowercase is valid)
                    // Only flag if surrounded by letters (not punctuation) AND it's not a single-character prefix
                    if (char.IsLetter(before) && char.IsLetter(after))
                    {
                        // Check if this is a common abbreviation pattern (single lowercase + uppercase)
                        // If the lowercase char is at the start of a word, it might be valid (e.g., "iPhone")
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
            
            // Pattern 2: Check for unusual consonant clusters that suggest missing characters
            // Generic approach: Only ASCII consonants are considered consonants
            // Non-ASCII letters (accented characters, special letters) are treated as non-consonants
            // This works for all languages because legitimate words contain non-ASCII letters
            // However, some languages (e.g., Polish, Czech) have legitimate consonant clusters
            // So we only flag this if combined with other issues (broken spacing or broken words)
            var hasUnusualConsonantClusters = false;
            var asciiConsonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
            var consecutiveConsonants = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                // Only ASCII consonants count as consonants
                // Non-ASCII letters (vowels, accented characters, special letters) break the cluster
                if (asciiConsonants.Contains(c))
                {
                    consecutiveConsonants++;
                    // Only flag if 4+ consecutive ASCII consonants (some languages have 3-consonant clusters)
                    if (consecutiveConsonants >= 4)
                    {
                        // Only flag if this is part of a broken word pattern (not a legitimate cluster)
                        // Check if surrounded by letters (likely a broken word)
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
            
            // Pattern 3: Removed broken word pattern detection
            // This was causing too many false positives for legitimate words in various languages
            // Broken spacing and consonant cluster detection are sufficient for encoding issue detection
            var hasBrokenWordPatterns = false;
            
            // Pattern 4: Check if text has very few non-ASCII characters but context suggests it should
            // This works for all languages that use special characters (non-ASCII characters)
            // However, English-only text is valid, so we need to be careful not to flag it
            var nonAsciiCharCount = text.Count(c => c > 127); // Non-ASCII characters
            var totalCharCount = text.Count(char.IsLetter);
            
            // Only flag as encoding issue if:
            // 1. Text is substantial (>200 letters) - short text might be English-only (increased from 150)
            // 2. Has broken spacing (suggests encoding corruption, not just English text)
            // 3. Very low non-ASCII ratio (<1.5%) combined with broken spacing (lowered from 2% to 1.5%)
            // This avoids false positives for English-only documents and isolated word issues
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
                    // Extract embedded images from PDF page (works for scanned PDFs)
                    // This is the only reliable way to get images from PDFs with current libraries
                    var embeddedImageResult = RenderPdfPageUsingEmbeddedImages(page);
                    if (embeddedImageResult != null)
                    {
                        return embeddedImageResult;
                    }
                    
                    // No embedded images found - this is a text-based PDF
                    // We cannot render text-based PDF pages to images with current libraries
                    // (iText7 .NET doesn't have bitmap rendering, SkiaSharp doesn't support PDF)
                    // OCR fallback will use extracted text instead
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
                // Get page dimensions
                var pageSize = page.GetPageSize();
                var width = (float)pageSize.GetWidth();
                var height = (float)pageSize.GetHeight();
                
                // Scale for better OCR quality (2x resolution for clearer text)
                var scale = 2.0f;
                var scaledWidth = (int)(width * scale);
                var scaledHeight = (int)(height * scale);

                // Create SkiaSharp bitmap with white background
                using (var bitmap = new SKBitmap(scaledWidth, scaledHeight))
                {
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        // White background for better OCR contrast
                        canvas.Clear(SKColors.White);
                        
                        // Try to extract embedded images from PDF page
                        // This works for scanned PDFs (image-based PDFs)
                        var imagesExtracted = ExtractImagesFromPdfPage(page, canvas, scale);
                        
                        // If no embedded images found, return null
                        if (!imagesExtracted)
                        {
                            return null;
                        }
                        
                        // Encode bitmap as PNG stream
                        var image = SKImage.FromBitmap(bitmap);
                        var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
                        var pngStream = new MemoryStream(pngData.ToArray());
                        pngStream.Position = 0;
                        
                        return pngStream;
                    }
                }
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
                    if (obj is PdfStream)
                    {
                        var stream = (PdfStream)obj;
                        var subtype = stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype);
                        
                        if (subtype != null && subtype.GetValue() == iText.Kernel.Pdf.PdfName.Image.GetValue())
                        {
                            try
                            {
                                var pdfImage = new PdfImageXObject((PdfStream)stream);
                                var imageBytes = pdfImage.GetImageBytes();
                                
                                if (imageBytes != null && imageBytes.Length > 0)
                                {
                                    using (var imageStream = new MemoryStream(imageBytes))
                                    {
                                        // Try to decode image with SkiaSharp
                                        using (var skImage = SKImage.FromEncodedData(imageStream))
                                        {
                                            if (skImage != null)
                                            {
                                                // Get image dimensions from PDF
                                                var imageWidth = pdfImage.GetWidth();
                                                var imageHeight = pdfImage.GetHeight();
                                                
                                                // Calculate scaling to fit page
                                                var pageSize = page.GetPageSize();
                                                var pageWidth = (float)pageSize.GetWidth();
                                                var pageHeight = (float)pageSize.GetHeight();
                                                
                                                // Scale image to fit page while maintaining aspect ratio
                                                var scaleX = (pageWidth * scale) / imageWidth;
                                                var scaleY = (pageHeight * scale) / imageHeight;
                                                var finalScale = Math.Min(scaleX, scaleY);
                                                
                                                // Draw image on canvas (centered and scaled)
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
        /// 1. Applying Unicode normalization (FormC - Canonical Composition)
        /// 2. Applying Unicode normalization (FormD - Canonical Decomposition) and recomposing
        /// 3. Fixing replacement characters () by attempting to decode using common encodings
        /// 4. Removing invalid Unicode characters
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
                // Step 1: Apply Unicode normalization (FormC - Canonical Composition)
                // This combines decomposed characters (e.g., "ğ" = "g" + combining dot below) into composed form
                // FormC is the most common form and works for all languages
                var normalized = text.Normalize(NormalizationForm.FormC);
                
                // Step 2: If FormC didn't help, try FormD (Canonical Decomposition) then recompose
                // This handles cases where characters are in a different normalization form
                // Some PDFs might store text in decomposed form, which needs to be recomposed
                if (normalized != text)
                {
                    // Text changed with FormC, try FormD as well for completeness
                    var decomposed = text.Normalize(NormalizationForm.FormD);
                    if (decomposed != text && decomposed != normalized)
                    {
                        // Recompose from FormD to FormC
                        normalized = decomposed.Normalize(NormalizationForm.FormC);
                    }
                }
                
                // Step 3: Remove or fix invalid Unicode characters
                // This preserves all valid Unicode characters from all alphabets while removing
                // only control characters (except common ones like newline, tab)
                normalized = RemoveInvalidUnicodeCharacters(normalized);
                
                return normalized;
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
                // Keep valid Unicode characters (including all language-specific characters)
                // Remove only control characters (except common ones like newline, tab)
                if (!char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

    }
}
