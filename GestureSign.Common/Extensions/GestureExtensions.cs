using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSign.Common.Extensions
{
    public static class GestureExtensions
    {
        public static int ImportGestures(this GestureManager gestureManager, IEnumerable<IGesture> gestures, IEnumerable<IApplication> relatedApplications)
        {
            int count = 0;
            if (gestures == null)
                return count;

            foreach (IGesture newGesture in gestures)
            {
                string matchName = gestureManager.GetMostSimilarGestureName(newGesture);
                if (!string.IsNullOrEmpty(matchName))
                {
                    if (relatedApplications != null && newGesture.Name != matchName)
                        relatedApplications.RenameGestures(newGesture.Name, matchName);
                }
                else
                {
                    if (gestureManager.GestureExists(newGesture.Name))
                    {
                        string newName = gestureManager.GetNewGestureName();
                        if (relatedApplications != null)
                            relatedApplications.RenameGestures(newGesture.Name, newName);
                        newGesture.Name = newName;
                    }
                    gestureManager.AddGesture(newGesture);
                    count++;
                }
            }

            if (count != 0)
            {
                gestureManager.SaveGestures();
            }
            return count;

        }
    }
}
