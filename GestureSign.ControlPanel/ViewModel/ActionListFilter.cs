using System;
using System.Text;

namespace GestureSign.ControlPanel.ViewModel
{
    public static class ActionListFilter
    {
        public const int AllFingers = 0;
        public const int MinimumFingerCount = 6;
        public static readonly string[] EmptyTokens = new string[0];

        public static bool MatchesFingerCount(int patternCount, int continuousContactCount, int selected)
        {
            if (selected <= AllFingers)
                return true;
            return MatchesOne(patternCount, selected) || MatchesOne(continuousContactCount, selected);
        }

        public static string BuildSearchKey(params string[] fields)
        {
            if (fields == null || fields.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field))
                    continue;
                if (builder.Length != 0)
                    builder.Append('\n');
                builder.Append(field);
            }
            return Compact(builder.ToString());
        }

        public static string[] Tokenize(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return EmptyTokens;

            var parts = filter.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var tokens = new string[parts.Length];
            int count = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var compact = Compact(parts[i]);
                if (compact.Length == 0)
                    continue;
                tokens[count++] = compact;
            }
            if (count == 0)
                return EmptyTokens;
            if (count == parts.Length)
                return tokens;

            var trimmed = new string[count];
            Array.Copy(tokens, trimmed, count);
            return trimmed;
        }

        public static bool MatchesKey(string searchKey, string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
                return true;
            if (string.IsNullOrEmpty(searchKey))
                return false;

            for (int i = 0; i < tokens.Length; i++)
            {
                if (searchKey.IndexOf(tokens[i], StringComparison.Ordinal) < 0)
                    return false;
            }
            return true;
        }

        public static bool SameTokens(string[] left, string[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool MatchesOne(int count, int selected)
        {
            if (count <= 0)
                return false;
            if (selected >= MinimumFingerCount)
                return count >= selected;
            return count == selected;
        }

        private static string Compact(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == ' ' || ch == '\t' || ch == '\r')
                    continue;
                builder.Append(char.ToLowerInvariant(ch));
            }
            return builder.ToString();
        }
    }
}
