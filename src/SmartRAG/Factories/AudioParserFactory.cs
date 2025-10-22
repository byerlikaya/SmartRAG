using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Services;
using System;

namespace SmartRAG.Factories
{
    /// <summary>
    /// Factory implementation for creating audio parser service instances
    /// </summary>
    public class AudioParserFactory : IAudioParserFactory
    {
        #region Fields

        private readonly IServiceProvider _serviceProvider;
        private readonly SmartRagOptions _options;

        #endregion

        #region Constructor

        public AudioParserFactory(IServiceProvider serviceProvider, IOptions<SmartRagOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates audio parser based on specified provider
        /// </summary>
        public IAudioParserService CreateAudioParser(AudioProvider provider)
        {
            if (provider == AudioProvider.GoogleCloud)
            {
                return _serviceProvider.GetRequiredService<GoogleAudioParserService>();
            }
            else if (provider == AudioProvider.Whisper)
            {
                return _serviceProvider.GetRequiredService<WhisperAudioParserService>();
            }
            else
            {
                throw new ArgumentException($"Unsupported audio provider: {provider}");
            }
        }

        /// <summary>
        /// Gets currently configured audio provider
        /// </summary>
        public AudioProvider GetCurrentProvider()
        {
            return _options.AudioProvider;
        }

        #endregion
    }
}

