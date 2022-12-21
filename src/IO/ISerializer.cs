﻿using Ocluse.LiquidSnow.Cryptography.IO.Internals;
using System.IO;
using System.Threading.Tasks;

namespace Ocluse.LiquidSnow.Cryptography.IO
{
    /// <summary>
    /// An interface that can be implemented to customize the process of deserialization and serialization of objects by the <see cref="ICryptoContainer"/> and <see cref="ICryptoFile"/> instances
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// This method is called to serialize an object passed in through the <see cref="CryptoFile.SerializeAsync{T}(T)"/> method and <see cref="CryptoContainer.AddAsync{T}(string, T, bool)"/>
        /// </summary>
        /// <param name="data">The object to be serialized</param>
        /// <param name="destinationStream">The stream that, after calling this method, will contain the serialzied data of the object</param>
        public Task SerializeAsync<T>(T data, Stream destinationStream) where T : class;

        /// <summary>
        /// This method is called to deserialize by <see cref="CryptoFile.DeserializeAsync{T}"/> and <see cref="CryptoContainer.GetAsync{T}(string)"/> methods
        /// </summary>
        /// <param name="sourceStream">A stream that will be passed by the methods containing the data to be deserialized</param>
        /// <returns></returns>
        public Task<T?> DeserializeAsync<T>(Stream sourceStream) where T : class;
    }
}
