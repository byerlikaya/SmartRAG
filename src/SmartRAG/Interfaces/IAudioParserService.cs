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


        #endregion
    }
}
