using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Hung.Base.Persistence;
using Hung.Data.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Hung.Data.Tests.Persistence
{
    public class PersistenceBenchmarkTests
    {
        private sealed class BenchmarkPayload
        {
            public string Content;
        }

        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath, "ComHungPersistenceBenchmarks", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            string fullRoot = Path.GetFullPath(root);
            string allowedRoot = Path.GetFullPath(Path.Combine(Application.temporaryCachePath, "ComHungPersistenceBenchmarks"));
            if (fullRoot.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
                Directory.Delete(fullRoot, true);
        }

        [TestCase(10 * 1024)]
        [TestCase(100 * 1024)]
        [TestCase(1024 * 1024)]
        public void SaveLoad_Benchmark_RecordsTwentyIterations(int targetBytes)
        {
            string key = "benchmark-" + targetBytes;
            var codec = new BeneficialCompressionCodec(16 * 1024);
            var definition = new SaveDefinition<BenchmarkPayload>(
                key,
                1,
                () => new BenchmarkPayload(),
                value => value?.Content == null
                    ? SaveValidationResult.Invalid("SAVE_VALIDATION_FAILED")
                    : SaveValidationResult.Valid(),
                Array.Empty<ISaveMigration>(),
                Array.Empty<string>(),
                codec,
                new Sha256SaveProtector(),
                SaveFailurePolicy.CreateDefaultAfterEvidencePreserved);
            var service = new PersistenceService(new FileSaveStore(root));
            var payload = new BenchmarkPayload { Content = DeterministicText(targetBytes) };

            service.Save(definition, payload);
            service.Load(definition);

            var saveTimes = new List<double>(20);
            var loadTimes = new List<double>(20);
            for (int i = 0; i < 20; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                SaveResult save = service.Save(definition, payload);
                stopwatch.Stop();
                Assert.That(save.Success, Is.True);
                saveTimes.Add(stopwatch.Elapsed.TotalMilliseconds);

                stopwatch.Restart();
                LoadResult<BenchmarkPayload> load = service.Load(definition);
                stopwatch.Stop();
                Assert.That(load.Success, Is.True);
                Assert.That(load.Value.Content.Length, Is.EqualTo(targetBytes));
                loadTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            string envelopeJson = File.ReadAllText(Path.Combine(root, "primary", key + ".save"));
            string encoding = SaveEnvelope.FromJson(envelopeJson).PayloadEncoding;
            double saveMedian = Median(saveTimes);
            double loadMedian = Median(loadTimes);
            double maximum = Math.Max(saveTimes.Max(), loadTimes.Max());
            UnityEngine.Debug.Log(
                $"PERSISTENCE_BENCHMARK bytes={targetBytes} encoding={encoding} " +
                $"saveMedianMs={saveMedian:F3} saveMaxMs={saveTimes.Max():F3} " +
                $"loadMedianMs={loadMedian:F3} loadMaxMs={loadTimes.Max():F3}");
            Assert.That(maximum, Is.LessThan(5000d));
        }

        private static string DeterministicText(int length)
        {
            const string pattern = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var builder = new StringBuilder(length);
            while (builder.Length < length)
                builder.Append(pattern);
            return builder.ToString(0, length);
        }

        private static double Median(IEnumerable<double> values)
        {
            double[] sorted = values.OrderBy(value => value).ToArray();
            int middle = sorted.Length / 2;
            return (sorted[middle - 1] + sorted[middle]) / 2d;
        }
    }
}
