
using System;
using UnityEngine;

namespace Yiliang.Tools
{
    [Serializable]
    public struct IntRect
    {
        [NonSerialized]
        public int x;
        [NonSerialized]
        public int y;

        [NonSerialized]
        public int width;
        [NonSerialized]
        public int height;

        public IntRect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static bool operator ==(IntRect rect1, IntRect rect2)
        {
            return rect1.x == rect2.x &&
                   rect1.y == rect2.y &&
                   rect1.width == rect2.width &&
                   rect1.height == rect2.height;
        }

        public static bool operator !=(IntRect rect1, IntRect rect2)
        {
            return rect1.x != rect2.x ||
                   rect1.y != rect2.y ||
                   rect1.width != rect2.width ||
                   rect1.height != rect2.height;
        }

        public override bool Equals(object obj)
        {
            IntRect otherRect = (IntRect)obj;

            return x == otherRect.x &&
                   y == otherRect.y &&
                   width == otherRect.width &&
                   height == otherRect.height;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (width.GetHashCode() << 2) ^ (y.GetHashCode() >> 2) ^ (height.GetHashCode() >> 1);
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3})", x, y, width, height);
        }

        public static IntRect zero  { get { return new IntRect(0, 0, 0, 0); } }
    }
}