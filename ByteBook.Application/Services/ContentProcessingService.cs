using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ByteBook.Application.Services;

public class ContentProcessingService : IContentProcessingService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentProcessingService> _logger;
    private readonly int _wordsPerPage;
    private readonly decimal _plagiarismThreshold;

    public ContentProcessingService(
        IFileStorageService fileStorageService,
        IConfiguration configuration,
        ILogger<ContentProcessingService> logger)
    {
        _fileStorageService = fileStorageService;
        _configuration = configuration;
        _logger = logger;
        _wordsPerPage = int.Parse(_configuration["ContentProcessing:WordsPerPage"] ?? "250");
        _plagiarismThreshold = decimal.Parse(_configuration["ContentProcessing:PlagiarismThreshold"] ?? "15.0");
    }

    public async Task<Result<ProcessedContentDto>> ProcessContentAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<ProcessedContentDto>.ValidationFailure("Content", "Content cannot be empty");
            }

            // Clean and format content
            var formattedContent = await FormatContentAsync(content, cancellationToken);
            if (formattedContent.IsFailure)
            {
                return Result<ProcessedContentDto>.Failure($"Content formatting failed: {formattedContent.ErrorMessage}");
            }

            // Calculate pages based on word count
            var wordCount = CountWords(formattedContent.Value!);
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)wordCount / _wordsPerPage));

            // Upload processed content to storage
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(formattedContent.Value!);
            using var contentStream = new MemoryStream(contentBytes);
            
            var fileName = $"content/{Guid.NewGuid()}.txt";
            var uploadResult = await _fileStorageService.UploadFileAsync(
                contentStream, 
                fileName, 
                "text/plain", 
                cancellationToken);

            if (uploadResult.IsFailure)
            {
                return Result<ProcessedContentDto>.Failure($"Content upload failed: {uploadResult.ErrorMessage}");
            }

            var result = new ProcessedContentDto
            {
                Content = formattedContent.Value!,
                TotalPages = totalPages,
                ContentUrl = uploadResult.Value!,
                Metadata = new Dictionary<string, object>
                {
                    { "WordCount", wordCount },
                    { "CharacterCount", formattedContent.Value!.Length },
                    { "ProcessedAt", DateTime.UtcNow },
                    { "ContentType", "text" }
                }
            };

            _logger.LogInformation("Content processed successfully: {WordCount} words, {Pages} pages", wordCount, totalPages);

            return Result<ProcessedContentDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing text content");
            return Result<ProcessedContentDto>.Failure("An error occurred while processing content");
        }
    }

    public async Task<Result<ProcessedContentDto>> ProcessPdfAsync(Stream pdfStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdfStream == null || pdfStream.Length == 0)
            {
                return Result<ProcessedContentDto>.ValidationFailure("File", "PDF file is empty or invalid");
            }

            // Upload original PDF to storage
            var uploadFileName = $"content/{Guid.NewGuid()}_{fileName}";
            var uploadResult = await _fileStorageService.UploadFileAsync(
                pdfStream, 
                uploadFileName, 
                "application/pdf", 
                cancellationToken);

            if (uploadResult.IsFailure)
            {
                return Result<ProcessedContentDto>.Failure($"PDF upload failed: {uploadResult.ErrorMessage}");
            }

            // Extract text content from PDF (simplified implementation)
            pdfStream.Position = 0;
            var extractedText = await ExtractTextFromPdf(pdfStream);
            
            // Estimate page count (for PDFs, we could get actual page count, but this is simplified)
            var wordCount = CountWords(extractedText);
            var estimatedPages = Math.Max(1, (int)Math.Ceiling((double)wordCount / _wordsPerPage));

            // Format extracted content
            var formattedContent = await FormatContentAsync(extractedText, cancellationToken);
            if (formattedContent.IsFailure)
            {
                _logger.LogWarning("Content formatting failed for PDF, using raw extracted text");
                formattedContent = Result<string>.Success(extractedText);
            }

            var result = new ProcessedContentDto
            {
                Content = formattedContent.Value!,
                TotalPages = estimatedPages,
                ContentUrl = uploadResult.Value!,
                Metadata = new Dictionary<string, object>
                {
                    { "OriginalFileName", fileName },
                    { "WordCount", wordCount },
                    { "CharacterCount", extractedText.Length },
                    { "ProcessedAt", DateTime.UtcNow },
                    { "ContentType", "pdf" },
                    { "FileSize", pdfStream.Length }
                }
            };

            _logger.LogInformation("PDF processed successfully: {FileName}, {WordCount} words, {Pages} pages", 
                fileName, wordCount, estimatedPages);

            return Result<ProcessedContentDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF file: {FileName}", fileName);
            return Result<ProcessedContentDto>.Failure("An error occurred while processing PDF file");
        }
    }

    public async Task<Result<PlagiarismCheckDto>> CheckPlagiarismAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<PlagiarismCheckDto>.ValidationFailure("Content", "Content cannot be empty");
            }

            // Simulate plagiarism check (in a real implementation, this would call an external service)
            await Task.Delay(1000, cancellationToken); // Simulate API call delay

            // Simple plagiarism simulation based on content patterns
            var suspiciousPatterns = new[]
            {
                "lorem ipsum",
                "copy and paste",
                "wikipedia",
                "sample text"
            };

            var matches = new List<PlagiarismMatch>();
            var totalSimilarity = 0m;

            foreach (var pattern in suspiciousPatterns)
            {
                if (content.ToLower().Contains(pattern))
                {
                    matches.Add(new PlagiarismMatch
                    {
                        Source = "Sample Database",
                        MatchedText = pattern,
                        SimilarityScore = 85.0m,
                        SourceUrl = "https://example.com/source"
                    });
                    totalSimilarity += 5.0m;
                }
            }

            // Add some randomness for demonstration
            var random = new Random();
            var randomSimilarity = (decimal)(random.NextDouble() * 10);
            totalSimilarity += randomSimilarity;

            var result = new PlagiarismCheckDto
            {
                IsPlagiarized = totalSimilarity > _plagiarismThreshold,
                SimilarityPercentage = Math.Min(totalSimilarity, 100m),
                Matches = matches,
                ReportUrl = matches.Any() ? "https://example.com/plagiarism-report" : null
            };

            _logger.LogInformation("Plagiarism check completed: {SimilarityPercentage}% similarity, {IsPlagiarized}", 
                result.SimilarityPercentage, result.IsPlagiarized);

            return Result<PlagiarismCheckDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing plagiarism check");
            return Result<PlagiarismCheckDto>.Failure("An error occurred during plagiarism check");
        }
    }

    public async Task<Result<string>> FormatContentAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<string>.ValidationFailure("Content", "Content cannot be empty");
            }

            await Task.Delay(100, cancellationToken); // Simulate AI processing delay

            // Basic content formatting
            var formatted = content.Trim();

            // Normalize line endings
            formatted = formatted.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove excessive whitespace
            formatted = Regex.Replace(formatted, @"\n\s*\n\s*\n", "\n\n");
            formatted = Regex.Replace(formatted, @"[ \t]+", " ");

            // Ensure proper paragraph spacing
            formatted = Regex.Replace(formatted, @"\n\n+", "\n\n");

            // Basic sentence formatting
            formatted = Regex.Replace(formatted, @"([.!?])\s*([A-Z])", "$1 $2");

            // Remove trailing whitespace from lines
            var lines = formatted.Split('\n');
            formatted = string.Join('\n', lines.Select(line => line.TrimEnd()));

            _logger.LogDebug("Content formatted successfully: {OriginalLength} -> {FormattedLength} characters", 
                content.Length, formatted.Length);

            return Result<string>.Success(formatted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting content");
            return Result<string>.Failure("An error occurred while formatting content");
        }
    }

    private async Task<string> ExtractTextFromPdf(Stream pdfStream)
    {
        // Simplified PDF text extraction
        // In a real implementation, you would use a library like iTextSharp, PdfPig, or similar
        await Task.Delay(500); // Simulate processing time

        // For demonstration, return placeholder text
        return @"This is extracted text from a PDF file. 
        
        In a real implementation, this would contain the actual text content extracted from the PDF using a proper PDF processing library.
        
        The text would maintain the original formatting and structure as much as possible while being suitable for further processing and display.
        
        This sample text represents what would be extracted from a typical PDF document containing readable text content.";
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Split by whitespace and count non-empty entries
        return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}