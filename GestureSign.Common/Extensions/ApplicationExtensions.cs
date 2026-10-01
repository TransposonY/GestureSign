using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSign.Common.Extensions
{
    public static class ApplicationExtensions
    {
        public static List<IGesture> GetRelatedGestures(this IEnumerable<IApplication> applications, IEnumerable<IGesture> gestures)
        {
            var result = new List<IGesture>();
            foreach (var app in applications)
            {
                if (app.Actions == null) continue;
                foreach (var action in app.Actions)
                {
                    if (action == null || string.IsNullOrEmpty(action.GestureName)) continue;
                    IGesture gesture = gestures.FirstOrDefault(g => g.Name == action.GestureName);
                    if (gesture != null && !result.Contains(gesture))
                        result.Add(gesture);
                }
            }
            return result;
        }

        public static void RenameGestures(this IEnumerable<IApplication> applications, string oldName, string newName)
        {
            foreach (var app in applications)
            {
                if (app.Actions == null) continue;
                foreach (var action in app.Actions)
                {
                    if (action.GestureName == oldName)
                        action.GestureName = newName;
                }
            }
        }

        public static List<ContinuousGesture> GetRelatedContinuousGestures(this IEnumerable<IApplication> applications, IEnumerable<ContinuousGesture> catalog)
        {
            var names = new HashSet<string>(applications.Where(app => app.Actions != null).SelectMany(app => app.Actions)
                .Where(a => a != null && !string.IsNullOrEmpty(a.ContinuousGestureName)).Select(a => a.ContinuousGestureName));
            return catalog.Where(g => names.Contains(g.Name)).ToList();
        }

        /// <summary>Applies all renames in one pass, so chained names (a→b, b→c) are not renamed twice.</summary>
        public static void RenameContinuousGestures(this IEnumerable<IApplication> applications, IDictionary<string, string> renames)
        {
            foreach (var action in applications.Where(app => app.Actions != null).SelectMany(app => app.Actions))
            {
                string newName;
                if (action?.ContinuousGestureName != null && renames.TryGetValue(action.ContinuousGestureName, out newName))
                    action.ContinuousGestureName = newName;
            }
        }
    }
}
