using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using Xunit;
using Newtonsoft.Json;

namespace GestureSign.Tests.Gestures
{
    public class ContinuousGestureCatalogTests : IDisposable
    {
        private const string LegacyType = "\"$type\":\"GestureSign.Common.Applications.ContinuousGesture, GestureSign.Common\"";

        private readonly string _file = Path.Combine(Path.GetTempPath(), "gs-continuous-" + Guid.NewGuid().ToString("N") + ".gsa");
        private readonly string _catalogFile = Path.Combine(Path.GetTempPath(), "gs-continuous-" + Guid.NewGuid().ToString("N") + ".gsc");

        public void Dispose()
        {
            if (File.Exists(_file))
                File.Delete(_file);
            if (File.Exists(_catalogFile))
                File.Delete(_catalogFile);
        }

        private static string LegacyAction(string name, int contactCount, int gesture)
        {
            return "{\"Name\":\"" + name + "\",\"Commands\":[{\"Name\":\"c\",\"IsEnabled\":true}],"
                + "\"ContinuousGesture\":{" + LegacyType + ",\"ContactCount\":" + contactCount + ",\"Gesture\":" + gesture + "}}";
        }

        /// <summary>Applications as stored by versions that embedded the continuous gesture in each action.</summary>
        private List<IApplication> LoadLegacyApplications(params string[] globalActions)
        {
            string json = "[{\"$type\":\"GestureSign.Common.Applications.GlobalApp, GestureSign.Common\",\"Name\":\"(Global Actions)\",\"Actions\":["
                + string.Join(",", globalActions) + "]}]";
            File.WriteAllText(_file, json);
            var applications = FileManager.LoadObject<List<IApplication>>(_file, false, true, true);
            Assert.NotNull(applications);
            return applications;
        }

        private static IAction Action(List<IApplication> applications, string name)
        {
            return applications.SelectMany(a => a.Actions).Single(a => a.Name == name);
        }

        private static List<IApplication> Applications(params IAction[] actions)
        {
            var app = new GlobalApp();
            foreach (var action in actions)
                app.AddAction(action);
            return new List<IApplication> { app };
        }

        [Fact]
        public void LegacyFileMovesContinuousGesturesIntoTheCatalog()
        {
            var applications = LoadLegacyApplications(
                LegacyAction("volume up", 5, 4),
                LegacyAction("volume down", 5, 8),
                LegacyAction("louder", 5, 4));
            var catalog = new List<ContinuousGesture>();

            Assert.True(ContinuousGestureCatalog.MigrateLegacy(applications, catalog));

            Assert.Equal(new[] { "5 fingers up", "5 fingers down" }, catalog.Select(g => g.Name));
            Assert.Equal(new[] { ContinuousDirection.Up, ContinuousDirection.Down }, catalog.Select(g => g.Direction));
            Assert.All(catalog, g => Assert.Equal(5, g.ContactCount));
            Assert.Equal("5 fingers up", Action(applications, "volume up").ContinuousGestureName);
            Assert.Equal("5 fingers down", Action(applications, "volume down").ContinuousGestureName);
            Assert.Equal("5 fingers up", Action(applications, "louder").ContinuousGestureName);
        }

        [Fact]
        public void MigratedApplicationsNoLongerCarryTheEmbeddedValue()
        {
            var applications = LoadLegacyApplications(LegacyAction("next", 3, 2));
            ContinuousGestureCatalog.MigrateLegacy(applications, new List<ContinuousGesture>());

            FileManager.SaveObject(applications, _file, true, true);
            string saved = File.ReadAllText(_file);
            var reloaded = FileManager.LoadObject<List<IApplication>>(_file, false, true, true);

            Assert.DoesNotContain("\"ContinuousGesture\"", saved);
            Assert.Equal("3 fingers right", Action(reloaded, "next").ContinuousGestureName);
            Assert.False(ContinuousGestureCatalog.MigrateLegacy(reloaded, new List<ContinuousGesture>()));
        }

        [Fact]
        public void LockedCatalogRetainsLegacyActionsAndRetryPersistsTheirMotion()
        {
            var applications = LoadLegacyApplications(LegacyAction("volume up", 3, 4));
            string originalActions = File.ReadAllText(_file);
            var catalog = new List<ContinuousGesture>();
            File.WriteAllText(_catalogFile, "[]");

            bool SaveCatalog()
            {
                try
                {
                    File.WriteAllText(_catalogFile, JsonConvert.SerializeObject(catalog));
                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
            }

            using (File.Open(_catalogFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.False(ContinuousGestureCatalog.TryMigrateLegacy(applications, catalog, SaveCatalog, out bool changed));
                Assert.False(changed);
                Assert.Null(Action(applications, "volume up").ContinuousGestureName);
                Assert.Equal(originalActions, File.ReadAllText(_file));
                Assert.Empty(ContinuousGestureManager.LoadFromFile(_catalogFile, true));
            }

            // The prepared entry survived in memory, so retry must save even though the catalog no longer grows.
            Assert.True(ContinuousGestureCatalog.TryMigrateLegacy(applications, catalog, SaveCatalog, out bool retriedChanges));
            Assert.True(retriedChanges);
            File.WriteAllText(_file, JsonConvert.SerializeObject(applications,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }));
            var reloadedApplications = FileManager.LoadObject<List<IApplication>>(_file, false, true, true);
            var reloadedCatalog = ContinuousGestureManager.LoadFromFile(_catalogFile, true);
            var binding = reloadedCatalog.Single(g => g.Name == Action(reloadedApplications, "volume up").ContinuousGestureName);
            Assert.Equal(3, binding.ContactCount);
            Assert.Equal(ContinuousDirection.Up, binding.Direction);
            Assert.DoesNotContain("\"ContinuousGesture\"", File.ReadAllText(_file));
            Assert.False(ContinuousGestureCatalog.MigrateLegacy(reloadedApplications, reloadedCatalog));
        }

        [Fact]
        public void PersistenceExceptionLeavesLegacyMotionAvailableForRetry()
        {
            var applications = LoadLegacyApplications(LegacyAction("volume up", 3, 4));
            var catalog = new List<ContinuousGesture>();

            Assert.Throws<IOException>(() =>
                ContinuousGestureCatalog.TryMigrateLegacy(applications, catalog,
                    () => throw new IOException("catalog unavailable"), out _));

            Assert.Null(Action(applications, "volume up").ContinuousGestureName);
            Assert.True(ContinuousGestureCatalog.MigrateLegacy(applications, catalog));
            var binding = catalog.Single(g => g.Name == Action(applications, "volume up").ContinuousGestureName);
            Assert.Equal(3, binding.ContactCount);
            Assert.Equal(ContinuousDirection.Up, binding.Direction);
        }

        [Fact]
        public void MigrationReusesTheCatalogEntryOfTheSameMotionAndAvoidsTakenNames()
        {
            var catalog = new List<ContinuousGesture>
            {
                new ContinuousGesture("switch desktop", 3, ContinuousDirection.Left),
                new ContinuousGesture("4 fingers up", 4, ContinuousDirection.Down),
            };
            var applications = LoadLegacyApplications(LegacyAction("prev", 3, 1), LegacyAction("show", 4, 4));

            ContinuousGestureCatalog.MigrateLegacy(applications, catalog);

            Assert.Equal("switch desktop", Action(applications, "prev").ContinuousGestureName);
            Assert.Equal("4 fingers up (2)", Action(applications, "show").ContinuousGestureName);
            Assert.Equal(3, catalog.Count);
        }

        [Theory]
        [InlineData(1, 4)]
        [InlineData(11, 4)]
        [InlineData(3, 3)]
        [InlineData(3, 15)]
        public void InvalidLegacyValuesAreDropped(int contactCount, int gesture)
        {
            var applications = LoadLegacyApplications(LegacyAction("odd", contactCount, gesture));
            var catalog = new List<ContinuousGesture>();

            Assert.True(ContinuousGestureCatalog.MigrateLegacy(applications, catalog));

            Assert.Empty(catalog);
            Assert.Null(Action(applications, "odd").ContinuousGestureName);
        }

        [Fact]
        public void ImportMapsKnownMotionsOntoExistingNamesAndRenamesCollidingNames()
        {
            var catalog = new List<ContinuousGesture>
            {
                new ContinuousGesture("a", 3, ContinuousDirection.Left),
                new ContinuousGesture("b", 3, ContinuousDirection.Right),
            };
            var sameMotionAsA = new Common.Applications.Action { Name = "x", ContinuousGestureName = "left3" };
            var newMotionNamedB = new Common.Applications.Action { Name = "y", ContinuousGestureName = "b" };
            var imported = new[]
            {
                new ContinuousGesture("left3", 3, ContinuousDirection.Left),
                new ContinuousGesture("b", 4, ContinuousDirection.Up),
            };

            int added = ContinuousGestureCatalog.Merge(catalog, imported, Applications(sameMotionAsA, newMotionNamedB));

            Assert.Equal(1, added);
            Assert.Equal("a", sameMotionAsA.ContinuousGestureName);
            Assert.Equal("4 fingers up", newMotionNamedB.ContinuousGestureName);
            Assert.Contains(catalog, g => g.Name == "4 fingers up" && g.ContactCount == 4 && g.Direction == ContinuousDirection.Up);
            Assert.Equal(3, catalog.Count);
        }

        [Fact]
        public void ImportRenamesEachReferenceOnlyOnce()
        {
            // Imported "a" maps onto existing "b"; imported "b" is a new motion whose name is taken and is renamed.
            // An action renamed a→b must not then be renamed b→(new name).
            var catalog = new List<ContinuousGesture> { new ContinuousGesture("b", 2, ContinuousDirection.Down) };
            var boundToA = new Common.Applications.Action { Name = "x", ContinuousGestureName = "a" };
            var boundToB = new Common.Applications.Action { Name = "y", ContinuousGestureName = "b" };
            var imported = new[]
            {
                new ContinuousGesture("a", 2, ContinuousDirection.Down),
                new ContinuousGesture("b", 2, ContinuousDirection.Up),
            };

            ContinuousGestureCatalog.Merge(catalog, imported, Applications(boundToA, boundToB));

            Assert.Equal("b", boundToA.ContinuousGestureName);
            Assert.Equal("2 fingers up", boundToB.ContinuousGestureName);
        }
    }
}
