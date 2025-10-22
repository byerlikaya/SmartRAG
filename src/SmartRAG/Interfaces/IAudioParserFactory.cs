using SmartRAG.Enums;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Factory for creating audio parser service instances based on provider
    /// </summary>
    public interface IAudioParserFactory
    {
        /// <summary>
        /// Creates audio parser based on specified provider
        /// </summary>
        /// <returns>Audio parser service instance</returns>
        IAudioParserService CreateAudioParser(AudioProvider provider);
        
        /// <summary>
        /// Gets currently configured audio provider
        /// </summary>
        /// <returns>Current audio provider</returns>
        AudioProvider GetCurrentProvider();
    }
}

