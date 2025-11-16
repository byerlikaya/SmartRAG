using SmartRAG.Enums;

namespace SmartRAG.Interfaces.Parser
{
    /// <summary>
    /// Factory for creating audio parser service instances based on provider
    /// </summary>
    public interface IAudioParserFactory
    {
        /// <summary>
        /// Creates audio parser based on specified provider
        /// </summary>
        /// <param name="provider">Audio provider type</param>
        /// <returns>Audio parser service instance</returns>
        IAudioParserService CreateAudioParser(AudioProvider provider);
    }
}

