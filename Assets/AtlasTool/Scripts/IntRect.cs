
using System.Runtime.Serialization;

namespace Elang.Tools
{
    [System.Serializable]
    public struct IntRect
    {
        public int x;
        public int y;

        public int width;
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
            return string.Format("({0}, {1}, {2}, {3})", x, y, width, height);
        }

        public static IntRect zero = new IntRect(0, 0, 0, 0);
    }

    [System.Serializable]
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