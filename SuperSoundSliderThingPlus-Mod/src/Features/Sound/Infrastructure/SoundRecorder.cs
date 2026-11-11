using System.Collections.Generic;
using System.Text;
using SuperSoundSliderThingPlus.Features.Sound.Domain;

namespace SuperSoundSliderThingPlus.Features.Sound.Infrastructure
{
    /// <summary>
    /// Remembers every distinct (sound, owner) pair the patches have classified, with its category and how often it was seen. Read-only
    /// evidence for the in-game tests and for the debug log: it is how the real names of the wolf pup / chicken love / shield generator
    /// sounds are confirmed in game. Bounded, main thread only.
    /// </summary>
    internal static class SoundRecorder
    {
        internal const int MaxEntries = 1000;
        private static readonly Dictionary<string, Entry> Seen = new Dictionary<string, Entry>();

        private sealed class Entry
        {
            internal SoundCategory Category;
            internal int Count;
        }

        /// <summary>How many different (sound, owner) pairs were recorded.</summary>
        internal static int Distinct => Seen.Count;

        /// <summary>Expected outcome: returns true the first time a pair is recorded (the caller logs it once), false for a repeat or when full.</summary>
        internal static bool Record(string sound, string owner, SoundCategory category)
        {
            string key = sound + " | " + owner;
            Entry entry;
            if (Seen.TryGetValue(key, out entry))
            {
                entry.Count++;
                return entry.Count == 1;
            }
            bool added = Seen.Count < MaxEntries;
            if (added) Seen[key] = new Entry { Category = category, Count = 1 };
            return added;
        }

        /// <summary>How many different pairs were classified into the category.</summary>
        internal static int DistinctIn(SoundCategory category)
        {
            int n = 0;
            foreach (Entry e in Seen.Values)
            {
                if (e.Category == category) n++;
            }
            return n;
        }

        /// <summary>Was a sound whose name contains the text recorded in the category?</summary>
        internal static bool Has(string soundPart, SoundCategory category)
        {
            foreach (KeyValuePair<string, Entry> pair in Seen)
            {
                if (pair.Value.Category == category && pair.Key.Contains(soundPart)) return true;
            }
            return false;
        }

        /// <summary>One line per category, e.g. "WolfPup 2, ChickenLove 1" - for test details and logs.</summary>
        internal static string Summary()
        {
            var sb = new StringBuilder();
            foreach (SoundCategory c in System.Enum.GetValues(typeof(SoundCategory)))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(c).Append(' ').Append(DistinctIn(c));
            }
            return sb.ToString();
        }

        /// <summary>Every recorded pair as "sound | owner = Category x count", separated by "; " (test logs).</summary>
        internal static string List()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, Entry> pair in Seen)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(pair.Key).Append(" = ").Append(pair.Value.Category).Append(" x").Append(pair.Value.Count);
            }
            return sb.ToString();
        }

        /// <summary>Forgets everything (tests).</summary>
        internal static bool Clear()
        {
            Seen.Clear();
            return Seen.Count == 0;
        }
    }
}
