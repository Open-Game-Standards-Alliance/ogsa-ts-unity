#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Ogs.Telemetry.Unity.Tests
{
    /// <summary>Encodes every conformance-corpus frame with the vendored
    /// core on Unity's runtime and byte-compares against the standard
    /// repo's committed corpus. Ignored when the corpus is not checked out
    /// beside (or above) the project.</summary>
    public class CorpusConformanceTests
    {
        static string? CorpusDir()
        {
            var env = Environment.GetEnvironmentVariable("OGS_TS_CONFORMANCE");
            if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (; dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "OGS-Telemetry-Standard", "conformance");
                if (File.Exists(Path.Combine(candidate, "manifest.json"))) return candidate;
            }
            return null;
        }

        static string[] VectorIds() =>
            ConformanceVectors.All().Select(v => v.Id).ToArray();

        [TestCaseSource(nameof(VectorIds))]
        public void Encodes_Byte_Identical_To_Corpus(string id)
        {
            var dir = CorpusDir();
            if (dir is null)
                Assert.Ignore("OGS-Telemetry-Standard/conformance not found (set OGS_TS_CONFORMANCE)");
            var hex = string.Concat(File.ReadAllLines(Path.Combine(dir!, "vectors", id + ".hex"))
                                        .Select(l => l.Trim()));
            var expected = new byte[hex.Length / 2];
            for (int i = 0; i < expected.Length; i++)
                expected[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);

            var actual = ConformanceVectors.All().Single(v => v.Id == id).Encode();
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
