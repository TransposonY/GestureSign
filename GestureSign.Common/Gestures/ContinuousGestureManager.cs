using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Extensions;
using Action = GestureSign.Common.Applications.Action;

namespace GestureSign.Common.Gestures
{
    /// <summary>
    /// Catalog of named continuous gestures, persisted in <see cref="Constants.ContinuousGesturesFileName"/>.
    /// ControlPanel and daemon each load their own copy; the ControlPanel notifies the daemon after <see cref="Saved"/>.
    /// </summary>
    public class ContinuousGestureManager
    {
        private static readonly Lazy<ContinuousGestureManager> LazyInstance = new Lazy<ContinuousGestureManager>(() => new ContinuousGestureManager());

        private List<ContinuousGesture> _gestures = new List<ContinuousGesture>();

        public static ContinuousGestureManager Instance => LazyInstance.Value;

        public static event EventHandler Saved;
        public static event EventHandler LoadCompleted;

        /// <summary>The initial load started by the constructor.</summary>
        public Task LoadingTask { get; }

        public IReadOnlyList<ContinuousGesture> ContinuousGestures => _gestures;

        protected ContinuousGestureManager()
        {
            LoadingTask = Load();
        }

        public Task Load()
        {
            return Task.Run(() =>
            {
                _gestures = LoadFromFile(Path.Combine(AppConfig.ApplicationDataPath, Constants.ContinuousGesturesFileName))
                    ?? LoadBackup()
                    ?? new List<ContinuousGesture>();
                LoadCompleted?.Invoke(this, EventArgs.Empty);
            });
        }

        public bool Save()
        {
            bool saved = FileManager.SaveObject(_gestures, Path.Combine(AppConfig.ApplicationDataPath, Constants.ContinuousGesturesFileName));
            if (saved)
                Saved?.Invoke(this, EventArgs.Empty);
            return saved;
        }

        public ContinuousGesture Find(string name)
        {
            return string.IsNullOrEmpty(name) ? null : _gestures.FirstOrDefault(g => g.Name == name);
        }

        /// <summary>Names of all catalog entries for this motion (normally at most one).</summary>
        public List<string> FindNames(int contactCount, ContinuousDirection direction)
        {
            return _gestures.Where(g => g.ContactCount == contactCount && g.Direction == direction).Select(g => g.Name).ToList();
        }

        public void Add(ContinuousGesture gesture)
        {
            _gestures.Add(gesture);
        }

        public void Remove(ContinuousGesture gesture)
        {
            _gestures.Remove(gesture);
        }

        public string GetNewName(int contactCount, ContinuousDirection direction)
        {
            return ContinuousGestureCatalog.CreateName(_gestures, contactCount, direction);
        }

        /// <summary>
        /// Persists catalog entries before replacing continuous gestures embedded in pre-catalog actions.
        /// Failed saves leave the actions unchanged so the migration can be retried.
        /// </summary>
        /// <returns>false when the catalog could not be saved; otherwise true.</returns>
        public bool TryAdoptLegacy(IEnumerable<IApplication> applications, out bool changed)
        {
            return ContinuousGestureCatalog.TryMigrateLegacy(applications, _gestures, Save, out changed);
        }

        /// <summary>Merges imported entries (see <see cref="ContinuousGestureCatalog.Merge"/>) and saves when any was added.</summary>
        /// <returns>Number of entries added.</returns>
        public int Import(IEnumerable<ContinuousGesture> gestures, IEnumerable<IApplication> relatedApplications)
        {
            if (gestures == null)
                return 0;
            int added = ContinuousGestureCatalog.Merge(_gestures, gestures, relatedApplications);
            if (added != 0)
                Save();
            return added;
        }

        public static List<ContinuousGesture> LoadFromFile(string filePath, bool throwException = false)
        {
            var gestures = FileManager.LoadObject<List<ContinuousGesture>>(filePath, false, false, throwException);
            gestures?.RemoveAll(g => g == null || string.IsNullOrEmpty(g.Name));
            return gestures;
        }

        private static List<ContinuousGesture> LoadBackup()
        {
            var directory = new DirectoryInfo(AppConfig.BackupPath);
            if (!directory.Exists)
                return null;
            foreach (var file in directory.EnumerateFiles("*" + Constants.ContinuousGesturesExtension).OrderByDescending(f => f.LastWriteTime))
            {
                var gestures = LoadFromFile(file.FullName);
                if (gestures != null)
                    return gestures;
            }
            return null;
        }
    }

    /// <summary>Catalog operations on plain lists, shared by the live catalog and archive imports.</summary>
    public static class ContinuousGestureCatalog
    {
        /// <summary>Unique default name for a motion, e.g. "3 fingers left" or "3 fingers left (2)".</summary>
        public static string CreateName(IEnumerable<ContinuousGesture> catalog, int contactCount, ContinuousDirection direction)
        {
            string baseName = contactCount + " fingers " + direction.ToString().ToLowerInvariant();
            var names = new HashSet<string>(catalog.Select(g => g.Name));
            string name = baseName;
            for (int i = 2; names.Contains(name); i++)
                name = baseName + " (" + i + ")";
            return name;
        }

        public static bool IsValidMotion(int contactCount, ContinuousDirection direction)
        {
            return contactCount >= ContinuousGesture.MinContactCount && contactCount <= ContinuousGesture.MaxContactCount
                && Enum.IsDefined(typeof(ContinuousDirection), direction);
        }

        /// <summary>
        /// Replaces continuous gestures embedded in actions (pre-catalog format) with a reference to a <paramref name="catalog"/> entry
        /// of the same motion, adding entries as needed. Invalid legacy values are dropped.
        /// </summary>
        /// <returns>true when any action changed.</returns>
        public static bool MigrateLegacy(IEnumerable<IApplication> applications, IList<ContinuousGesture> catalog)
        {
            TryMigrateLegacy(applications, catalog, null, out bool changed);
            return changed;
        }

        /// <summary>
        /// Prepares legacy catalog entries, persists them when a new reference is needed, then updates actions.
        /// A failed save retains the embedded values and the prepared entries for a subsequent retry.
        /// Passing no save callback migrates a plain list, as used by archive imports.
        /// </summary>
        /// <returns>false when persistence failed; <paramref name="changed"/> is true only after actions were migrated.</returns>
        public static bool TryMigrateLegacy(IEnumerable<IApplication> applications, IList<ContinuousGesture> catalog,
            Func<bool> saveCatalog, out bool changed)
        {
            changed = false;
            var pending = new Dictionary<Action, string>();
            bool needsCatalogSave = false;
            foreach (var action in applications.Where(app => app?.Actions != null).SelectMany(app => app.Actions).OfType<Action>())
            {
                var legacy = action.LegacyContinuousGesture;
                if (legacy == null)
                    continue;

                string name = action.ContinuousGestureName;
                if (string.IsNullOrEmpty(name) && IsValidMotion(legacy.ContactCount, legacy.Gesture))
                {
                    var entry = catalog.FirstOrDefault(g => g.ContactCount == legacy.ContactCount && g.Direction == legacy.Gesture);
                    if (entry == null)
                    {
                        entry = new ContinuousGesture(CreateName(catalog, legacy.ContactCount, legacy.Gesture), legacy.ContactCount, legacy.Gesture);
                        catalog.Add(entry);
                    }
                    name = entry.Name;
                    // Persist even reused entries: a prior failed migration may have added them only in memory.
                    needsCatalogSave = true;
                }
                pending[action] = name;
            }

            if (needsCatalogSave && saveCatalog != null && !saveCatalog())
                return false;

            foreach (var migration in pending)
            {
                migration.Key.ContinuousGestureName = migration.Value;
                migration.Key.LegacyContinuousGesture = null;
            }
            changed = pending.Count != 0;
            return true;
        }

        /// <summary>
        /// Merges imported entries into <paramref name="catalog"/>: an entry whose motion already exists maps onto the existing name,
        /// a new motion whose name is taken gets a fresh name. References in <paramref name="relatedApplications"/> follow.
        /// </summary>
        /// <returns>Number of entries added.</returns>
        public static int Merge(IList<ContinuousGesture> catalog, IEnumerable<ContinuousGesture> imported, IEnumerable<IApplication> relatedApplications)
        {
            var renames = new Dictionary<string, string>();
            int added = 0;
            foreach (var gesture in imported.Where(g => g != null && !string.IsNullOrEmpty(g.Name) && IsValidMotion(g.ContactCount, g.Direction)))
            {
                var existing = catalog.FirstOrDefault(g => g.IsSameMotion(gesture));
                if (existing != null)
                {
                    renames[gesture.Name] = existing.Name;
                    continue;
                }

                string name = catalog.Any(g => g.Name == gesture.Name) ? CreateName(catalog, gesture.ContactCount, gesture.Direction) : gesture.Name;
                renames[gesture.Name] = name;
                catalog.Add(new ContinuousGesture(name, gesture.ContactCount, gesture.Direction));
                added++;
            }

            relatedApplications?.RenameContinuousGestures(renames);
            return added;
        }
    }
}
