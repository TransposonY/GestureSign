using System;

namespace GestureSign.Common.Exceptions
{
    public class FileWriteException : Exception
    {
        public FileWriteException(Exception innerException) : base(null, innerException)
        {
        }
    }
}
