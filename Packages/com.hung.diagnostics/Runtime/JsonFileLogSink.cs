using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Hung.Diagnostics
{
    public sealed class JsonFileLogSink : IDisposable
    {
        private readonly ConcurrentQueue<string> queue = new();
        private readonly string directoryPath;
        private readonly int flushIntervalMs;
        private readonly long maxFileSizeBytes;

        private StreamWriter writer;
        private Thread workerThread;
        private volatile bool running;
        private int fileIndex;
        private string currentFilePath;

        public JsonFileLogSink(
            string directoryPath,
            string sessionId,
            int flushIntervalMs,
            int maxFileSizeMb)
        {
            this.directoryPath = directoryPath;
            this.flushIntervalMs = Mathf.Max(100, flushIntervalMs);
            this.maxFileSizeBytes = Mathf.Max(1, maxFileSizeMb) * 1024L * 1024L;

            Directory.CreateDirectory(directoryPath);

            OpenNewFile(sessionId);

            running = true;
            workerThread = new Thread(WriteLoop)
            {
                IsBackground = true,
                Name = "GameDiagnostics.JsonFileLogSink"
            };
            workerThread.Start();
        }

        public string CurrentFilePath => currentFilePath;

        public void Enqueue(string jsonLine)
        {
            if (string.IsNullOrWhiteSpace(jsonLine))
                return;

            queue.Enqueue(jsonLine);
        }

        private void WriteLoop()
        {
            while (running)
            {
                FlushQueue();
                Thread.Sleep(flushIntervalMs);
            }

            FlushQueue();
            writer?.Flush();
            writer?.Dispose();
        }

        private void FlushQueue()
        {
            try
            {
                while (queue.TryDequeue(out string line))
                {
                    writer.WriteLine(line);

                    if (writer.BaseStream.Length >= maxFileSizeBytes)
                    {
                        RotateFile();
                    }
                }

                writer.Flush();
            }
            catch
            {
                // Không gọi Debug.Log ở đây để tránh recursive log.
            }
        }

        private void RotateFile()
        {
            writer.Flush();
            writer.Dispose();

            fileIndex++;

            string sessionPrefix = Path.GetFileNameWithoutExtension(currentFilePath);
            int partIndex = sessionPrefix.IndexOf("_part", StringComparison.Ordinal);
            if (partIndex >= 0)
            {
                sessionPrefix = sessionPrefix.Substring(0, partIndex);
            }

            currentFilePath = Path.Combine(
                directoryPath,
                $"{sessionPrefix}_part{fileIndex}.jsonl"
            );

            writer = new StreamWriter(currentFilePath, append: false, Encoding.UTF8);
        }

        private void OpenNewFile(string sessionId)
        {
            fileIndex = 0;
            currentFilePath = Path.Combine(directoryPath, $"{sessionId}.jsonl");
            writer = new StreamWriter(currentFilePath, append: true, Encoding.UTF8);
        }

        public void Dispose()
        {
            running = false;

            if (workerThread != null && workerThread.IsAlive)
            {
                workerThread.Join(1000);
                return; // WriteLoop's own exit path already flushed+disposed writer.
            }

            // Worker never started/already exited on its own — safe to clean up here.
            writer?.Flush();
            writer?.Dispose();
        }
    }
}