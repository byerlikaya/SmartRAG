using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Service interface for parsing audio files and extracting text content through speech recognition
    /// </summary>
    public interface IAudioParserService : IDisposable
    {
        #region Public Methods

        /// <summary>
        /// Transcribes audio content from a stream to text using default options
        /// </summary>
        /// <param name="audioStream">The audio stream to transcribe</param>
        /// <param name="fileName">The name of the audio file for format detection</param>
        /// <param name="language">Language code for transcription (e.g., "tr-TR", "en-US")</param>
        /// <returns>Audio transcription result containing text and metadata</returns>
        Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null);

        /// <summary>
        /// Transcribes audio content from a stream to text using custom options
        /// </summary>
        /// <param name="audioStream">The audio stream to transcribe</param>
        /// <param name="options">Custom transcription options</param>
        /// <returns>Audio transcription result containing text and metadata</returns>
        Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, AudioTranscriptionOptions options);

        /// <summary>
        /// Checks if the given file name represents a supported audio format
        /// </summary>
        /// <param name="fileName">The file name to check</param>
        /// <returns>True if the format is supported, false otherwise</returns>
        bool IsSupportedFormat(string fileName);

        /// <summary>
        /// Gets a list of supported audio file extensions
        /// </summary>
        /// <returns>Collection of supported file extensions</returns>
        IEnumerable<string> GetSupportedFormats();

        /// <summary>
        /// Gets a list of supported MIME content types for audio
        /// </summary>
        /// <returns>Collection of supported MIME content types</returns>
        IEnumerable<string> GetSupportedContentTypes();

        #endregion
    }
}
