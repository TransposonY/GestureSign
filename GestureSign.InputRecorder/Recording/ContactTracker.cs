using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>The path of one contact from touch-down to lift, in normalized surface coordinates.</summary>
    public sealed class ContactStroke
    {
        public ContactStroke(int contactId)
        {
            ContactId = contactId;
        }

        public int ContactId { get; }

        public List<PointF> Points { get; } = new List<PointF>();

        /// <summary>The contact was lifted (tip off, or missing from a complete frame).</summary>
        public bool Ended { get; internal set; }
    }

    /// <summary>
    /// Follows contacts across <see cref="DecodedReport"/>s of one device. Parallel and hybrid reporting are
    /// handled like the daemon: a report with a non-zero Contact Count starts a frame and only that many slots
    /// (possibly spread over several reports) are valid. A contact is down while its tip is on (a pen also
    /// while it is in range); when a frame completes, contacts it did not mention are treated as lifted.
    /// </summary>
    public sealed class ContactTracker
    {
        private readonly bool _pen;
        private readonly Dictionary<int, ContactStroke> _active = new Dictionary<int, ContactStroke>();
        private readonly HashSet<int> _seenInFrame = new HashSet<int>();
        private readonly List<ContactStroke> _missing = new List<ContactStroke>();
        private int _remaining;

        public ContactTracker(bool pen)
        {
            _pen = pen;
        }

        /// <summary>Every stroke since the last <see cref="Clear"/>, in touch-down order.</summary>
        public List<ContactStroke> Strokes { get; } = new List<ContactStroke>();

        public IEnumerable<ContactStroke> Active => _active.Values;

        public int ActiveCount => _active.Count;

        /// <summary>Highest number of simultaneous contacts since the last <see cref="Clear"/>.</summary>
        public int MaxActive { get; private set; }

        public bool ButtonDown { get; private set; }

        public void Apply(DecodedReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            int valid;
            if (report.ContactCount == null)
            {
                valid = report.Slots.Length;
                _seenInFrame.Clear();
            }
            else
            {
                if (report.ContactCount.Value > 0)
                {
                    _remaining = report.ContactCount.Value;
                    _seenInFrame.Clear();
                }
                valid = Math.Min(_remaining, report.Slots.Length);
                _remaining -= valid;
            }
            ButtonDown = report.ButtonDown;

            for (int i = 0; i < valid; i++)
            {
                DecodedContact contact = report.Slots[i];
                _seenInFrame.Add(contact.Id);
                var point = new PointF((float)contact.X, (float)contact.Y);
                ContactStroke stroke;
                if (contact.Tip || _pen && contact.InRange)
                {
                    if (!_active.TryGetValue(contact.Id, out stroke))
                    {
                        stroke = new ContactStroke(contact.Id);
                        _active.Add(contact.Id, stroke);
                        Strokes.Add(stroke);
                    }
                    stroke.Points.Add(point);
                }
                else if (_active.TryGetValue(contact.Id, out stroke))
                {
                    stroke.Points.Add(point);
                    End(stroke);
                }
            }

            bool frameComplete = report.ContactCount == null ? valid > 0 : valid > 0 && _remaining == 0;
            if (frameComplete)
            {
                _missing.Clear();
                foreach (ContactStroke stroke in _active.Values)
                {
                    if (!_seenInFrame.Contains(stroke.ContactId))
                        _missing.Add(stroke);
                }
                foreach (ContactStroke stroke in _missing)
                    End(stroke);
                // Only a complete frame has removed contacts left over from the preceding frame.
                if (_active.Count > MaxActive)
                    MaxActive = _active.Count;
            }
        }

        private void End(ContactStroke stroke)
        {
            stroke.Ended = true;
            _active.Remove(stroke.ContactId);
        }

        public void Clear()
        {
            _active.Clear();
            _seenInFrame.Clear();
            Strokes.Clear();
            _remaining = 0;
            MaxActive = 0;
            ButtonDown = false;
        }
    }
}
