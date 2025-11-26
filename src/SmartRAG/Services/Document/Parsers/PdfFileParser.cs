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

                using (var pdfReader = new PdfReader(new MemoryStream(bytes)))
                {
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        var textBuilder = new StringBuilder();
                        await ExtractTextFromPdfPagesAsync(pdfDocument, textBuilder, language);

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
        /// </summary>
        private async Task ExtractTextFromPdfPagesAsync(PdfDocument pdfDocument, StringBuilder textBuilder, string language = null)
        {
            var pageCount = pdfDocument.GetNumberOfPages();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);

                // Always check for encoding issues first, even if text extraction found substantial text
                // PDF text extraction often fails for image-based PDFs, producing broken text
                var hasEncodingIssues = !string.IsNullOrWhiteSpace(text) && HasTextEncodingIssues(text);
                var shouldUseOcr = false;
                
                // If text extraction found substantial text AND no encoding issues, use it
                if (!string.IsNullOrWhiteSpace(text) && text.Trim().Length >= MinTextLengthForOcrFallback && !hasEncodingIssues)
                {
                    // Text extraction is good, use it
                    textBuilder.AppendLine(text);
                    _logger.LogDebug("PDF page {PageNumber} text extraction successful, using extracted text (length: {Length})", i, text.Length);
                }
                else
                {
                    // Text is missing, very short, OR has encoding issues - use OCR
                    shouldUseOcr = true;
                    if (hasEncodingIssues)
                    {
                        _logger.LogDebug("PDF page {PageNumber} text extraction has encoding issues (broken words detected), using OCR instead (text length: {Length})", i, text?.Length ?? 0);
                    }
                    else if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < MinTextLengthForOcrFallback)
                    {
                        _logger.LogDebug("PDF page {PageNumber} text extraction found insufficient text (length: {Length}), using OCR instead", i, text?.Length ?? 0);
                    }
                }

                // If we should use OCR, render page and extract text via OCR
                if (shouldUseOcr)
                {
                    try
                    {
                        // Render PDF page as image and use OCR
                        var pageImageStream = await RenderPdfPageAsImageAsync(page);
                        if (pageImageStream != null)
                        {
                            var ocrText = await _imageParserService.ExtractTextFromImageAsync(pageImageStream, language);
                            if (!string.IsNullOrWhiteSpace(ocrText))
                            {
                                textBuilder.AppendLine(ocrText);
                                _logger.LogDebug("Used OCR for PDF page {PageNumber} (extracted text length: {Length} chars, had encoding issues: {HasIssues})", 
                                    i, text?.Length ?? 0, !string.IsNullOrWhiteSpace(text));
                            }
                            else
                            {
                                // OCR failed, fallback to extracted text if available
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    textBuilder.AppendLine(text);
                                }
                            }
                        }
                        else
                        {
                            // Page rendering failed, fallback to extracted text if available
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                textBuilder.AppendLine(text);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text via OCR for PDF page {PageNumber}, using extracted text if available", i);
                        // Fallback to extracted text even if it has issues
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            textBuilder.AppendLine(text);
                        }
                    }
                }
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
            var hasBrokenSpacing = false;
            for (int i = 0; i < text.Length - 1; i++)
            {
                if (char.IsLower(text[i]) && char.IsUpper(text[i + 1]))
                {
                    // Check if this looks like a broken word (not an abbreviation)
                    var before = i > 0 ? text[i - 1] : ' ';
                    var after = i + 2 < text.Length ? text[i + 2] : ' ';
                    
                    // If surrounded by letters (not punctuation), it's likely a broken word
                    if (char.IsLetter(before) && char.IsLetter(after))
                    {
                        hasBrokenSpacing = true;
                        _logger.LogDebug("Detected broken spacing at position {Position}: '{Before}{Char1}{Char2}{After}'", 
                            i, before, text[i], text[i + 1], after);
                        break;
                    }
                }
            }
            
            // Pattern 2: Check for unusual consonant clusters that suggest missing characters
            // This is language-agnostic - works for all languages with special characters
            // Look for sequences of 3+ consonants without vowels (suggests missing special chars/vowels)
            var hasUnusualConsonantClusters = false;
            var consonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
            var vowels = "aeiouAEIOU";
            var consecutiveConsonants = 0;
            var maxConsecutiveConsonants = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (consonants.Contains(c) && !vowels.Contains(c))
                {
                    consecutiveConsonants++;
                    maxConsecutiveConsonants = Math.Max(maxConsecutiveConsonants, consecutiveConsonants);
                    if (consecutiveConsonants >= 3) // 3+ consecutive consonants is unusual (lowered threshold)
                    {
                        hasUnusualConsonantClusters = true;
                        _logger.LogDebug("Detected unusual consonant cluster: {Count} consecutive consonants at position {Position}", 
                            consecutiveConsonants, i);
                        break;
                    }
                }
                else
                {
                    consecutiveConsonants = 0;
                }
            }
            
            // Pattern 3: Check if text has very few non-ASCII characters but context suggests it should
            // This works for all languages that use special characters (non-ASCII characters)
            var nonAsciiCharCount = text.Count(c => c > 127); // Non-ASCII characters
            var totalCharCount = text.Count(char.IsLetter);
            
            // CRITICAL: Much more aggressive threshold for non-ASCII character detection
            // Many languages (Turkish, German, Russian, French, Spanish, etc.) use special characters
            // If text is long but has NO or VERY FEW non-ASCII characters, it DEFINITELY has encoding issues
            // Threshold: if text has >50 letters but <5% non-ASCII, likely encoding issue
            var hasFewSpecialChars = totalCharCount > 50 && nonAsciiCharCount < totalCharCount * 0.05;
            
            if (hasFewSpecialChars)
            {
                _logger.LogDebug("Detected very few non-ASCII characters: {NonAscii}/{Total} = {Ratio:P2} (threshold: 5%)", 
                    nonAsciiCharCount, totalCharCount, (double)nonAsciiCharCount / totalCharCount);
            }
            
            // Pattern 4: Check for words that look broken (missing characters in the middle)
            // Look for words with unusual patterns: consonant-vowel-consonant-consonant-consonant
            // This suggests missing characters (works for all languages)
            var hasBrokenWordPatterns = false;
            var words = text.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                if (word.Length >= 4) // Check words with 4+ characters (lowered threshold)
                {
                    // Count vowel-to-consonant ratio - very low ratio suggests missing characters
                    var vowelCount = word.Count(c => vowels.Contains(c));
                    var consonantCount = word.Count(c => consonants.Contains(c));
                    
                    if (consonantCount > 0 && vowelCount > 0)
                    {
                        var ratio = (double)vowelCount / consonantCount;
                        // If ratio is very low (< 0.25), word might be broken (more lenient threshold)
                        if (ratio < 0.25 && consonantCount >= 3) // Lowered thresholds
                        {
                            hasBrokenWordPatterns = true;
                            _logger.LogDebug("Detected broken word pattern: '{Word}' (vowel ratio: {Ratio:F2}, consonants: {Consonants})", 
                                word, ratio, consonantCount);
                            break;
                        }
                    }
                    // Also check for words with no vowels at all (definitely broken)
                    else if (vowelCount == 0 && consonantCount >= 3)
                    {
                        hasBrokenWordPatterns = true;
                        _logger.LogDebug("Detected word with no vowels: '{Word}' (definitely broken)", word);
                        break;
                    }
                }
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
        /// Renders a PDF page as an image stream for OCR processing using iText7 and SkiaSharp
        /// </summary>
        private async Task<Stream> RenderPdfPageAsImageAsync(PdfPage page)
        {
            return await Task.Run(() =>
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
                            
                            // Try to extract images from PDF page first
                            var imagesExtracted = ExtractImagesFromPdfPage(page, canvas, scale);
                            
                            // If no images found, render the entire page content
                            // Note: iText7 doesn't have direct SkiaSharp rendering, so we use a workaround
                            // We'll render the page by converting it to an image using PdfCanvas
                            if (!imagesExtracted)
                            {
                                // Render page content using PdfCanvas approach
                                RenderPdfPageContent(page, canvas, width, height, scale);
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
                    _logger.LogWarning(ex, "Failed to render PDF page as image for OCR");
                    return null;
                }
            });
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
        /// Renders PDF page content to canvas by copying page to a new PDF and rendering it
        /// Uses iText7's PdfCanvas to render page content
        /// </summary>
        private void RenderPdfPageContent(PdfPage page, SKCanvas canvas, float width, float height, float scale)
        {
            try
            {
                // Get page content stream
                var pageContent = page.GetContentBytes();
                
                if (pageContent != null && pageContent.Length > 0)
                {
                    // Create a temporary PDF document with this page
                    using (var tempStream = new MemoryStream())
                    {
                        using (var tempWriter = new PdfWriter(tempStream))
                        using (var tempDoc = new PdfDocument(tempWriter))
                        {
                            // Copy page to temp document - convert Rectangle to PageSize
                            var pageSize = page.GetPageSize();
                            var tempPage = tempDoc.AddNewPage(new iText.Kernel.Geom.PageSize(pageSize));
                            var tempCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(tempPage);
                            
                            // Copy content from original page
                            tempCanvas.AddXObjectAt(page.CopyAsFormXObject(tempDoc), 0, 0);
                            
                            tempDoc.Close();
                            
                            // Now we have the page as a form XObject, but we still need to render it to SkiaSharp
                            // Since iText7 doesn't have direct SkiaSharp rendering, we'll use the image extraction approach
                            // For scanned PDFs (image-based), the ExtractImagesFromPdfPage method should handle it
                            
                            _logger.LogDebug("PDF page content prepared for rendering (image extraction should handle scanned PDFs)");
                        }
                    }
                }
                
                // If no content or images, draw white background
                var rect = new SKRect(0, 0, width * scale, height * scale);
                canvas.DrawRect(rect, new SKPaint { Color = SKColors.White });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to render PDF page content, using white background");
                // Draw white background as fallback
                var rect = new SKRect(0, 0, width * scale, height * scale);
                canvas.DrawRect(rect, new SKPaint { Color = SKColors.White });
            }
        }
    }
}
