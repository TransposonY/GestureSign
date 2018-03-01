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
    }
}
