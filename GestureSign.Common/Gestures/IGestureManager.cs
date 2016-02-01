using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestureSign.Common.Gestures
{
	public interface IGestureManager
	{
		void DeleteGesture(string gestureName);
		bool GestureExists(string gestureName);
		string GestureName { get; set; }
		IGesture[] Gestures { get; }
		string[] GetAvailableGestures();
        string GetGestureName(List<List<System.Drawing.Point>> Points);
		string GetGestureSetNameMatch(List<List<System.Drawing.Point>> Points);
		IGesture GetNewestGestureSample(string gestureName);
		void AddGesture(IGesture Gesture);
        Task<bool> LoadGestures();
		bool SaveGestures(bool notice);
	}
}
