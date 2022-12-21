﻿using Ocluse.LiquidSnow.Core.Extensions;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ocluse.LiquidSnow.Cryptography.IO.Internals
{
    internal class CryptoFile : ICryptoFile
    {
        #region Private Fields
        private byte[] _key;

        private readonly Stream _stream;
        #endregion

        #region Constructors

        public CryptoFile(string key)
        {
            _stream = new MemoryStream();
            _key = key.GetBytes<UTF8Encoding>();
        }

        public CryptoFile(string key, Stream stream)
        {
            _stream = stream;
            _key = key.GetBytes<UTF8Encoding>();
        }

        public CryptoFile(string key, string path)
        {
            _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _key = key.GetBytes<UTF8Encoding>();
        }
        #endregion

        #region Public Methods
        public void SetKey(string key)
        {
            _key = key.GetBytes<UTF8Encoding>();
        }

        public void SetKey(byte[] key)
        {
            Array.Clear(_key, 0, _key.Length);
            Array.Copy(key, 0, _key, 0, key.Length);
        }

        public async Task<T?> DeserializeAsync<T>() where T : class
        {
            using MemoryStream msData = new MemoryStream();

            await ReadAsync(msData);
            msData.Position = 0;

            return await IOSettings.Serializer.DeserializeAsync<T>(msData);
        }

        public async Task SerializeAsync<T>(T o) where T : class
        {
            using MemoryStream msData = new MemoryStream();

            await IOSettings.Serializer.SerializeAsync(o, msData).ConfigureAwait(false);

            msData.Position = 0;

            await WriteAsync(msData);
        }

        public async Task WriteAsync(Stream stream, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            _stream.Position = 0;
            _stream.SetLength(0);

            await IOSettings.Algorithm.EncryptAsync(input: stream, output: _stream, _key, progress, cancellationToken);
        }

        public async Task ReadAsync(Stream stream, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            _stream.Position = 0;
            await IOSettings.Algorithm.DecryptAsync(input: _stream, output: stream, _key, progress, cancellationToken);
        }

        public async Task WriteBytesAsync(byte[] buffer, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            using MemoryStream msData = new MemoryStream(buffer);
            await WriteAsync(msData, progress, cancellationToken);

        }

        public async Task<byte[]> ReadBytesAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            using MemoryStream ms = new MemoryStream();
            await ReadAsync(ms, progress, cancellationToken);
            return ms.ToArray();
        }

        public async Task WriteTextAsync(string contents, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            await WriteBytesAsync(contents.GetBytes<UTF8Encoding>(), progress, cancellationToken);
        }

        public async Task<string> ReadTextAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            byte[] data = await ReadBytesAsync(progress, cancellationToken);
            return data.GetString<UTF8Encoding>();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        #endregion

    }
}