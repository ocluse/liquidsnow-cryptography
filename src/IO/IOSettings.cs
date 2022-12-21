using Ocluse.LiquidSnow.Cryptography.IO.Internals;
using Ocluse.LiquidSnow.Cryptography.Symmetrics;

namespace Ocluse.LiquidSnow.Cryptography.IO
{
    /// <summary>
    /// A static class for specifying the cryptographic and serialization settings to be used by the <see cref="ICryptoFile"/> and <see cref="ICryptoContainer"/> instances
    /// </summary>
    public static class IOSettings
    {
        /// <summary>
        /// The serializer that is used to convert an <see cref="object"/> to a data stream that will be stored and reverse.
        /// </summary>
        /// <remarks>
        /// If one is not specified, the default <see cref="System.Text.Json.JsonSerializer"/> is used.
        /// </remarks>
        public static ISerializer Serializer { get; set; } = new InternalSerializer();

        /// <summary>
        /// The <see cref="ISymmetric"/> algorithm that will be used 
        /// to encrypt and decrypt by the <see cref="ICryptoFile"/> and <see cref="ICryptoContainer"/> instances
        /// </summary>
        /// <remarks>
        /// If one is not manually assigned, the default algorithm is created using <see cref="SymmetricBuilder.CreateAesFixed"/> method
        /// </remarks>
        public static ISymmetric Algorithm { get; set; } = SymmetricBuilder.CreateAesFixed();
    }
}
