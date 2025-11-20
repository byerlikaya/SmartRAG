using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Models;
using SmartRAG.Services.Parser;
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
        /// Creates audio parser - only Whisper.net is supported
        /// </summary>
        public IAudioParserService CreateAudioParser(AudioProvider provider)
        {
            // Always use Whisper.net - only supported audio provider
            return _serviceProvider.GetRequiredService<WhisperAudioParserService>();
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

