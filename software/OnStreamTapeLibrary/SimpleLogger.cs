using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OnStreamTapeLibrary
{
    /// <summary>
    /// A simple <see cref="ILogger"/> implementation which writes to the console.
    /// </summary>
    public class SimpleLogger : ILogger, IDisposable
    {
        private readonly object _lock = new ();
        private readonly bool _showDebugMessages;
        private bool _disposed;
        private volatile Task _currentTask = Task.CompletedTask;
        
        public SimpleLogger(bool debugMode = false) {
            this._showDebugMessages = debugMode;
        }

        /// <inheritdoc cref="ILogger.BeginScope{TState}"/>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull {
            return this;
        }

        /// <inheritdoc cref="ILogger.IsEnabled"/>
        public bool IsEnabled(LogLevel logLevel) {
            if (logLevel == LogLevel.Debug && !this._showDebugMessages)
                return false;
            
            lock (this._lock)
                return !this._disposed;
        }

        /// <inheritdoc cref="ILogger.Log{TState}"/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string>? formatter) {
            if (formatter == null || (logLevel == LogLevel.Debug && !this._showDebugMessages))
                return;

            lock (_lock) {
                if (this._disposed)
                    return;

                string n = Environment.NewLine;
                string exc = "";
                if (exception != null)
                    exc = n + n + exception.GetType() + ": " + exception.Message + n + exception.StackTrace + n;
                string fullMessage = $"[{DateTime.Now}/{logLevel.ToString()[0]}]: {formatter(state, exception)}{exc}";

                this._currentTask = this._currentTask.ContinueWith(_ => this.LogFinalMessageAsync(fullMessage).Wait());
            }
        }

        /// <summary>
        /// Handles the logging of the string.
        /// </summary>
        /// <param name="message"></param>
        protected virtual async Task LogFinalMessageAsync(string message) {
            await Console.Out.WriteLineAsync(message).ConfigureAwait(false);
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
                this._currentTask.Wait();
                this.OnDispose();
            }
        }
    }

    /// <summary>
    /// A simple <see cref="ILogger"/> implementation which writes to a file as well as the console.
    /// </summary>
    public class FileLogger : SimpleLogger
    {
        private readonly StreamWriter _writer;

        public FileLogger(string path, bool debugMode = false, bool overwriteIfExists = false) : base(debugMode) {
            if (overwriteIfExists && File.Exists(path))
                File.Delete(path);

            FileMode mode = overwriteIfExists ? FileMode.Create : FileMode.Append;
            this._writer = new StreamWriter(new BufferedStream(new FileStream(path, mode, FileAccess.Write)), Encoding.UTF8);
        }

        /// <inheritdoc cref="SimpleLogger.LogFinalMessageAsync"/>
        protected override async Task LogFinalMessageAsync(string message) {
            await base.LogFinalMessageAsync(message).ConfigureAwait(false);
            await this._writer.WriteLineAsync(message).ConfigureAwait(false);
        }
        
        /// <inheritdoc cref="SimpleLogger.OnDispose"/>
        protected override void OnDispose() {
            base.OnDispose();
            this._writer.Dispose();
        }
    }
}