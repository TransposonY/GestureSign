using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GestureSign.Tests.Gestures
{
    public class LegacyMigrationSerializationTests : IDisposable
    {
        private readonly string _file = Path.Combine(Path.GetTempPath(), "gs-legacy-export-" + Guid.NewGuid().ToString("N") + ".gsa");

        public void Dispose()
        {
            if (File.Exists(_file))
                File.Delete(_file);
        }

        private List<IApplication> LoadLegacyApplications()
        {
            File.WriteAllText(_file,
                "[{\"$type\":\"GestureSign.Common.Applications.GlobalApp, GestureSign.Common\","
                + "\"Name\":\"(Global Actions)\",\"Actions\":[{\"Name\":\"volume up\",\"Commands\":[],"
                + "\"ContinuousGesture\":{\"$type\":\"GestureSign.Common.Applications.ContinuousGesture, GestureSign.Common\","
                + "\"ContactCount\":3,\"Gesture\":4}}]}]");
            return FileManager.LoadObject<List<IApplication>>(_file, false, true, true);
        }

        private static IAction OnlyAction(List<IApplication> applications)
        {
            return applications.Single().Actions.Single();
        }

        [Fact]
        public void DeferredMigrationSurvivesApplicationExportAndMigratesAfterReload()
        {
            var applications = LoadLegacyApplications();
            var unavailableCatalog = new List<ContinuousGesture>();

            Assert.False(ContinuousGestureCatalog.TryMigrateLegacy(applications, unavailableCatalog,
                () => false, out bool changed));
            Assert.False(changed);
            Assert.Null(OnlyAction(applications).ContinuousGestureName);

            // Archive exports and settings backups serialize the current application objects.
            Assert.True(FileManager.SaveObject(applications, _file, true, true));
            var exportedAction = JArray.Parse(File.ReadAllText(_file))[0]["Actions"][0];
            Assert.Equal(3, (int)exportedAction["ContinuousGesture"]["ContactCount"]);
            Assert.Equal((int)ContinuousDirection.Up, (int)exportedAction["ContinuousGesture"]["Gesture"]);
            Assert.Null(exportedAction["ContinuousGesture"]["$type"]);

            var reloaded = FileManager.LoadObject<List<IApplication>>(_file, false, true, true);
            var recoveredCatalog = new List<ContinuousGesture>();
            Assert.True(ContinuousGestureCatalog.TryMigrateLegacy(reloaded, recoveredCatalog,
                () => true, out bool recoveredChanges));
            Assert.True(recoveredChanges);
            var binding = Assert.Single(recoveredCatalog);
            Assert.Equal(binding.Name, OnlyAction(reloaded).ContinuousGestureName);
            Assert.Equal(3, binding.ContactCount);
            Assert.Equal(ContinuousDirection.Up, binding.Direction);
        }

        [Fact]
        public void SuccessfulMigrationSerializesOnlyTheCatalogReference()
        {
            var applications = LoadLegacyApplications();
            var catalog = new List<ContinuousGesture>();
            Assert.True(ContinuousGestureCatalog.TryMigrateLegacy(applications, catalog,
                () => true, out bool changed));
            Assert.True(changed);
            var binding = Assert.Single(catalog);

            Assert.True(FileManager.SaveObject(applications, _file, true, true));
            var savedAction = JArray.Parse(File.ReadAllText(_file))[0]["Actions"][0];
            Assert.Null(savedAction["ContinuousGesture"]);
            Assert.Equal(binding.Name, (string)savedAction["ContinuousGestureName"]);

            // The compatibility property must also omit null with serializers that include nulls by default.
            var serializedAction = JObject.Parse(JsonConvert.SerializeObject(OnlyAction(applications),
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }));
            Assert.Null(serializedAction["ContinuousGesture"]);
            Assert.Equal(binding.Name, (string)serializedAction["ContinuousGestureName"]);

            var reloaded = FileManager.LoadObject<List<IApplication>>(_file, false, true, true);
            Assert.Equal(binding.Name, OnlyAction(reloaded).ContinuousGestureName);
            Assert.False(ContinuousGestureCatalog.MigrateLegacy(reloaded, new List<ContinuousGesture>()));
        }
    }
}
