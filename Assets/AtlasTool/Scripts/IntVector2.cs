using System;
using UnityEngine;

namespace Yiliang.Tools
{
    [Serializable]
    public struct IntVector2
    {
        public int x;

        public int y;

        public IntVector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(IntVector2 vector1, IntVector2 vector2)
        {
            return vector1.x == vector2.x &&
                   vector1.y == vector2.y;
        }

        public static bool operator !=(IntVector2 vector1, IntVector2 vector2)
        {
            return vector1.x != vector2.x ||
                   vector1.y != vector2.y;
        }

        public override bool Equals(object obj)
        {
            IntVector2 otherVector2 = (IntVector2)obj;

            return x == otherVector2.x &&
                   y == otherVector2.y;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", x, y);
        }
    }
}