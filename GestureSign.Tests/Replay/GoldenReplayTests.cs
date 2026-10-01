using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using GestureSign.Common.Gestures;
using GestureSign.InputRecorder.Recording;
using Xunit;

namespace GestureSign.Tests.Replay
{
    /// <summary>
    /// Golden-master tests: every Fixtures/Replay/**/*.gsrec.json is replayed through the input pipeline and
    /// its transcript must equal the sibling *.expected.json. Both files are language-neutral so a port can
    /// run the same corpus. Set GESTURESIGN_UPDATE_GOLDEN=1 to (re)write expected files and synthetic recordings.
    /// </summary>
    public class GoldenReplayTests
    {
        private const string RecordingSuffix = ".gsrec.json";
        private const string ExpectedSuffix = ".expected.json";

        internal static bool UpdateGolden => Environment.GetEnvironmentVariable("GESTURESIGN_UPDATE_GOLDEN") == "1";

        internal static string FixtureDirectory([CallerFilePath] string sourceFile = null)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile), "..", "Fixtures", "Replay"));
        }

        private static readonly Lazy<List<IGesture>> Gestures = new Lazy<List<IGesture>>(() =>
            GestureManager.LoadGesturesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Gestures", "Defaults.gest"), throwException: true));

        public static IEnumerable<object[]> Recordings()
        {
            if (UpdateGolden)
                SyntheticRecordings.WriteAll(FixtureDirectory());

            string root = FixtureDirectory();
            if (!Directory.Exists(root))
                yield break;
            foreach (var file in Directory.EnumerateFiles(root, "*" + RecordingSuffix, SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
                yield return new object[] { file.Substring(root.Length + 1).Replace('\\', '/') };
        }

        [Theory]
        [MemberData(nameof(Recordings))]
        public void ReplayMatchesGolden(string recordingName)
        {
            string recordingPath = Path.Combine(FixtureDirectory(), recordingName);
            string expectedPath = recordingPath.Substring(0, recordingPath.Length - RecordingSuffix.Length) + ExpectedSuffix;

            string actual = InputReplayer.Replay(InputRecording.Load(recordingPath), Gestures.Value);

            if (UpdateGolden)
            {
                File.WriteAllText(expectedPath, actual, new UTF8Encoding(false));
                return;
            }

            Assert.True(File.Exists(expectedPath), $"Missing golden file {expectedPath}. Run test.ps1 with GESTURESIGN_UPDATE_GOLDEN=1 and review the new file.");
            string expected = File.ReadAllText(expectedPath).Replace("\r\n", "\n");
            if (expected == actual)
                return;

            string actualPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(expectedPath).Replace(ExpectedSuffix, ".actual.json"));
            File.WriteAllText(actualPath, actual, new UTF8Encoding(false));
            Assert.Fail(FirstDifference(expected, actual) + $"\nFull transcript written to {actualPath}");
        }

        private static string FirstDifference(string expected, string actual)
        {
            var e = expected.Split('\n');
            var a = actual.Split('\n');
            for (int i = 0; i < Math.Max(e.Length, a.Length); i++)
            {
                string el = i < e.Length ? e[i] : "<end>";
                string al = i < a.Length ? a[i] : "<end>";
                if (el != al)
                    return $"Transcript differs at line {i + 1}:\n  expected: {el}\n  actual:   {al}";
            }
            return "Transcripts differ.";
        }
    }
}
