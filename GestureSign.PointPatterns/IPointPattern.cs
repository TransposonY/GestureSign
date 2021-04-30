using System.Drawing;

namespace GestureSign.PointPatterns
{
    public interface IPointPattern
    {
        #region Interface Properties

        Point[][] Points { get; set; }

        #endregion
    }
}
