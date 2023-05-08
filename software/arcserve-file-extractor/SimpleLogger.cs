using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// A simple logger which writes to the console.
    /// </summary>
    public class SimpleLogger : ILogger, IDisposable
    {
        private readonly object _lock = new object();
        private bool _disposed;

        /// <inheritdoc cref="ILogger.BeginScope{TState}"/>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull {
            return this;
        }

        /// <inheritdoc cref="ILogger.IsEnabled"/>
        public bool IsEnabled(LogLevel logLevel) {
            lock (this._lock)
                return !this._disposed;
        }

        /// <inheritdoc cref="ILogger.Log{TState}"/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string>? formatter) {
            if (formatter == null)
                return;

            lock (_lock) {
                if (this._disposed)
                    return;

                var n = Environment.NewLine;
                string exc = "";
                if (exception != null)
                    exc = n + n + exception.GetType() + ": " + exception.Message + n + exception.StackTrace + n;
                string fullMessage = $"[{DateTime.Now}/{logLevel.ToString()[0]}]: {formatter(state, exception)}{exc}";

                this.LogFinalMessage(fullMessage);
            }
        }

        /// <summary>
        /// Handles the logging of the string.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void LogFinalMessage(string message) {
            Console.WriteLine(message);
        }
        
        /// <summary>
        /// Called upon disposal.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        protected virtual void OnDispose() {
            this._disposed = true;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() {
            lock (_lock) {
                this.OnDispose();
            }
        }
    }

    /// <summary>
    /// A simple logger which writes to a file as well as the console.
    /// </summary>
    public class FileLogger : SimpleLogger
    {
        private readonly DataWriter _writer;

        public FileLogger(string path, bool overwriteIfExists = false) {
            if (overwriteIfExists && File.Exists(path))
                File.Delete(path);

            FileMode mode = overwriteIfExists ? FileMode.Create : FileMode.Append;
            this._writer = new DataWriter(new BufferedStream(new FileStream(path, mode, FileAccess.Write)));
        }

        /// <inheritdoc cref="SimpleLogger.LogFinalMessage"/>
        protected override void LogFinalMessage(string message) {
            this._writer.WriteStringBytes(message);
            this._writer.Write('\n');
            base.LogFinalMessage(message);
        }
        
        /// <inheritdoc cref="SimpleLogger.OnDispose"/>
        protected override void OnDispose() {
            base.OnDispose();
            this._writer.Dispose();
        }
    }
}