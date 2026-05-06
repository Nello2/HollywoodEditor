using PropertyChanged;
using System.Collections.Generic;

namespace HollywoodEditor.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Milestones
    {
        public string id { get; set; }
        public string group { get; set; }
        public bool finished { get; set; }
        public bool locked { get; set; }
        public double progress { get; set; }
        public List<object> chains { get; set; }
        public int Inner_id => int.Parse(id.Substring(id.Length - 1, 1));
        public string Inner_name => id.Remove(id.Length - 2, 2);

        public static bool operator ==(Milestones a, Milestones b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

            return b.id == a.id &&
                   b.group == a.group &&
                   b.finished == a.finished &&
                   b.locked == a.locked &&
                   b.progress == a.progress;
        }

        public static bool operator !=(Milestones a, Milestones b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return this == (Milestones)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (id != null ? id.GetHashCode() : 0);
                hash = hash * 23 + (group != null ? group.GetHashCode() : 0);
                hash = hash * 23 + finished.GetHashCode();
                hash = hash * 23 + locked.GetHashCode();
                hash = hash * 23 + progress.GetHashCode();
                return hash;
            }
        }
    }
}