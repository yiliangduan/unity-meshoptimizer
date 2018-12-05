//
// 参考: https://github.com/juj/RectangleBinPack
//

using System;
using System.Collections.Generic;
using UnityEngine;

public class MaxRectsBinPack {

    /// <summary>
    /// 允许翻转
    /// </summary>
    public bool bAllowFlip;

    /// <summary>
    /// 宽度
    /// </summary>
    public int mWidth;

    /// <summary>
    /// 高度
    /// </summary>
    public int mHeight;

    /// <summary>
    /// 已经使用的Rect
    /// </summary>
    private List<Rect> mUsedRectangles = new List<Rect>();

    /// <summary>
    /// 还未使用的Rect
    /// </summary>
    private List<Rect> mFreeRectRangles = new List<Rect>();


    public enum FreeRectChoiceHeuristic
    {
        RectBestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
		RectBestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
		RectBestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
		RectBottomLeftRule, ///< -BL: Does the Tetris placement.
		RectContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
	};

    public MaxRectsBinPack(int width, int height, bool allowFilp)
    {
        bAllowFlip = allowFilp;
        mWidth = width;
        mHeight = height;

        mUsedRectangles.Clear();
        mFreeRectRangles.Clear();

        Rect rect = new Rect(0, 0, width, height);
        mFreeRectRangles.Add(rect);
    }

    /// <summary>
    /// 插入一个Node
    /// </summary>
    public Rect Insert(int width, int height, FreeRectChoiceHeuristic freeRectChoiceHeuristic)
    {
        Rect newNode = Rect.zero;

        // Unused in this function. We don't need to know the score after finding the position.
        int score1 = int.MaxValue;
        int score2 = int.MaxValue;

        switch (freeRectChoiceHeuristic)
        {
            case FreeRectChoiceHeuristic.RectBestShortSideFit:
                 newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
                 break;
            case FreeRectChoiceHeuristic.RectBestLongSideFit:
                 newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
                 break;
            case FreeRectChoiceHeuristic.RectContactPointRule:
                newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                break;
            case FreeRectChoiceHeuristic.RectBottomLeftRule:
                newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score1, ref score2);
                break;
            case FreeRectChoiceHeuristic.RectBestAreaFit:
                newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
                break;
        }

        if (newNode.height == 0)
            return newNode;

        int numRectanglesToProcess = mFreeRectRangles.Count;

        for (int i=0; i<numRectanglesToProcess; ++i)
        {
            if (SplitFreeNode(mFreeRectRangles[i], newNode))
            {
                mFreeRectRangles.RemoveAt(i);

                --i;
                --numRectanglesToProcess;
            }
        }

        PruneFreeList();

        mUsedRectangles.Add(newNode);

        return newNode;
    }

    public void Insert(List<Vector2> rects, List<Rect> dst, FreeRectChoiceHeuristic freeRectChoiceHeuristic)
    {
        dst.Clear();

        while (rects.Count > 0)
        {
            int bestScore1 = int.MaxValue;
            int bestScore2 = int.MaxValue;

            int bestRectIndex = -1;

            Rect bestNode = Rect.zero;

            for (int i=0; i<rects.Count; ++i)
            {
                int score1 = int.MaxValue;
                int score2 = int.MaxValue;

                Rect newNode = ScoreRect((int)rects[i].x, (int)rects[i].y, freeRectChoiceHeuristic, ref score1, ref score2);

                if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                {
                    bestScore1 = score1;
                    bestScore2 = score2;

                    bestNode = newNode;
                    bestRectIndex = i;
                }
            }

            if (bestRectIndex == -1 || bestNode == Rect.zero)
                return;

            PlaceRect(bestNode);

            dst.Add(bestNode);
            rects.RemoveAt(bestRectIndex);
        }
    }

    public bool Layout(float x, float y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return false;

        Rect newNode = new Rect(x, y, width, height);

        int numRectanglesToProcess = mFreeRectRangles.Count;

        for (int i = 0; i < numRectanglesToProcess; ++i)
        {
            if (SplitFreeNode(mFreeRectRangles[i], newNode))
            {
                mFreeRectRangles.RemoveAt(i);

                --i;
                --numRectanglesToProcess;
            }
        }

        PruneFreeList();

        mUsedRectangles.Add(newNode);

        return true;
    }

    /// <summary>
    /// 找到一块 短边(取width, height其中短的) 填充率最高的剩余空间。
    /// </summary>
    /// <returns></returns>
    public Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
    {
        Rect bestNode = Rect.zero;

        bestShortSideFit = int.MaxValue;
        bestLongSideFit = int.MaxValue;

        for (int i=0; i < mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                int leftoverHoriz = (int)Mathf.Abs(mFreeRectRangles[i].width - width);
                int leftoverVert = (int)Mathf.Abs(mFreeRectRangles[i].height - height);

                int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                int longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

                if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }

            if (bAllowFlip && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                int flippedLeftoverHoriz = (int)Mathf.Abs(mFreeRectRangles[i].width - height);
                int flippedLeftoverVert = (int)Mathf.Abs(mFreeRectRangles[i].height - width);

                int flippedShortSideFit = Mathf.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                int flippedLongSideFit = Mathf.Max(flippedLeftoverHoriz, flippedLeftoverVert);

                if (flippedShortSideFit < bestShortSideFit || (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = height;
                    bestNode.height = width;

                    bestShortSideFit = flippedShortSideFit;
                    bestLongSideFit = flippedLongSideFit;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    /// 找到一块 长边(取width, height其中长的) 填充率最高的剩余空间填充
    /// </summary>
    public Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
    {
        Rect bestNode = Rect.zero;

        bestShortSideFit = int.MaxValue;
        bestLongSideFit = int.MaxValue;

        for (int i=0; i<mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                int leftoverHoriz = (int)Mathf.Abs(mFreeRectRangles[i].width - width);
                int leftoverVert = (int)Mathf.Abs(mFreeRectRangles[i].height - height);

                int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                int longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

                if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }

            if (bAllowFlip && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                int leftoverHoriz = (int)Mathf.Abs(mFreeRectRangles[i].width - height);
                int leftoverVert = (int)Mathf.Abs(mFreeRectRangles[i].height - width);

                int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                int longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

                if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = height;
                    bestNode.height = width;

                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }
        }

        return bestNode;
    }


    /// <summary>
    /// 按照找到的剩余空间块的左下角坐标来计算，加上高度之后取y值越小得那个空闲块
    /// </summary>
    public Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestX, ref int bestY)
    {
        Rect bestNode = Rect.zero;

        bestY = int.MaxValue;
        bestX = int.MaxValue;

        for (int i=0; i<mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                int topSideY = (int)mFreeRectRangles[i].y + height;
                if (topSideY < bestY || topSideY == bestY && mFreeRectRangles[i].x < bestX)
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestY = topSideY;
                    bestX = (int)mFreeRectRangles[i].x;
                }
            }

            if (bAllowFlip && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                int topSideY = (int)mFreeRectRangles[i].y + width;
                if (topSideY < bestY || (topSideY == bestY && mFreeRectRangles[i].x < bestX))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = height;
                    bestNode.height = width;

                    bestY = topSideY;
                    bestX = (int)mFreeRectRangles[i].x;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    /// 根据长，宽的比例相加结果为因子来计算最优
    /// </summary>
    public Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
    {
        Rect bestNode = Rect.zero;

        bestContactScore = -1;

        for (int i=0; i< mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                int score = ContactPointScoreNode((int)mFreeRectRangles[i].x, (int)mFreeRectRangles[i].y, width, height);
                if (score > bestContactScore)
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestContactScore = score;
                }
            }

            if (bAllowFlip && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                int score = ContactPointScoreNode((int)mFreeRectRangles[i].x, (int)mFreeRectRangles[i].y, height, width);

                if (score > bestContactScore)
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = height;
                    bestNode.height = width;

                    bestContactScore = score;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    /// 根据面积占空闲块的比例来取最优
    /// </summary>
    public Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
    {
        Rect bestNode = Rect.zero;

        bestAreaFit = int.MaxValue;
        bestShortSideFit = int.MaxValue;

        for (int i=0; i < mFreeRectRangles.Count; ++i)
        {
            int areaFit = (int)(mFreeRectRangles[i].width * mFreeRectRangles[i].height - width * height);

            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                int leftoverHoriz = (int)Mathf.Abs(mFreeRectRangles[i].width - width);
                int leftoverVert = (int)Mathf.Abs(mFreeRectRangles[i].height - height);

                int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);

                if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestShortSideFit = shortSideFit;
                    bestAreaFit = areaFit;
                }
            }

            if (bAllowFlip && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                int leftoverHoriz = (int)Mathf.Abs(mFreeRectRangles[i].width - height);
                int leftoverVert = (int)Mathf.Abs(mFreeRectRangles[i].height - width);

                int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);

                if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;
                    bestNode.width = height;
                    bestNode.height = width;
                    bestShortSideFit = shortSideFit;
                    bestAreaFit = areaFit;
                }
            }
        }

        return bestNode;
    }

    private int ContactPointScoreNode(int x, int y, int width, int height)
    {
        int score = 0;

        if (0 == x || x + width == mWidth)
        {
            score += height;
        }

        if (y == 0 || y + height == mHeight)
        {
            score += width;
        }

        for (int i=0; i<mUsedRectangles.Count; ++i)
        {
            if (mUsedRectangles[i].x == x + width || mUsedRectangles[i].x + mUsedRectangles[i].width == x)
            {
                score += CommonIntervalLength((int)mUsedRectangles[i].y, (int)(mUsedRectangles[i].y + mUsedRectangles[i].height), y, y + height);
            }

            if (mUsedRectangles[i].y == y + height || mUsedRectangles[i].y + mUsedRectangles[i].height == y)
            {
                score += CommonIntervalLength((int)mUsedRectangles[i].x, (int)(mUsedRectangles[i].x + mUsedRectangles[i].width), x, x + width);
            }
        }

        return score;
    }

    private int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
    {
        if (i1end < i2start || i2end < i1start)
            return 0;

        return Mathf.Min(i1end, i2end) - Mathf.Max(i1start, i2start);
    }

    /// <summary>
    /// 分割分配的一个空闲空间
    /// </summary>
    private bool SplitFreeNode(Rect freeNode, Rect usedNode)
    {
        //空闲空间和分配的空间位置不相交，则直接返回
        if (usedNode.x >= freeNode.x + freeNode.width || usedNode.x + usedNode.width <= freeNode.x ||
            usedNode.y >= freeNode.y + freeNode.height || usedNode.y + usedNode.height <= freeNode.y)
        {
            return false;
        }

        //x相交
        if (usedNode.x < freeNode.x + freeNode.width && usedNode.x + usedNode.width > freeNode.x)
        {
            if (usedNode.y > freeNode.y && usedNode.y < freeNode.y + freeNode.height)
            {
                Rect newNode = freeNode;
                newNode.height = usedNode.y - newNode.y;
                mFreeRectRangles.Add(newNode);
            }

            if (usedNode.y + usedNode.height < freeNode.y + freeNode.height)
            {
                Rect newNode = freeNode;
                newNode.y = usedNode.y + usedNode.height;
                newNode.height = freeNode.y + freeNode.height - (usedNode.y + usedNode.height);

                mFreeRectRangles.Add(newNode);
            }
        }

        //y相交
        if (usedNode.y < freeNode.y + freeNode.height && usedNode.y + usedNode.height > freeNode.y)
        {
            if (usedNode.x > freeNode.x && usedNode.x < freeNode.x + freeNode.width)
            {
                Rect newNode = freeNode;

                newNode.width = usedNode.x - newNode.x;
                mFreeRectRangles.Add(newNode);
            }

            if (usedNode.x + usedNode.width < freeNode.x + freeNode.width)
            {
                Rect newNode = freeNode;
                newNode.x = usedNode.x + usedNode.width;
                newNode.width = freeNode.x + freeNode.width - (usedNode.x + usedNode.width);

                mFreeRectRangles.Add(newNode);
            }
        }

        return true;
    }

    /// <summary>
    /// 删除剩余空间中多余的Rect
    /// </summary>
    private void PruneFreeList()
    {
        for (int i=0; i<mFreeRectRangles.Count; ++i)
        {
            for (int j=i+1; j<mFreeRectRangles.Count; ++j)
            {
                if (RectIsContainedIn(mFreeRectRangles[i], mFreeRectRangles[j]))
                {
                    mFreeRectRangles.RemoveAt(i);

                    --i;
                    break;
                }

                if (RectIsContainedIn(mFreeRectRangles[j], mFreeRectRangles[i]))
                {
                    mFreeRectRangles.RemoveAt(j);

                    --j;
                }
            }
        }
    }

    /// <summary>
    /// 是否包含
    /// </summary>
    private bool RectIsContainedIn(Rect a, Rect b)
    {
        return a.x >= b.x && a.y >= b.y &&
               a.x + a.width <= b.x + b.width &&
               a.y + a.height <= b.y + b.height;
    }

    /// <summary>
    /// Rect的匹配度计算
    /// </summary>
    private Rect ScoreRect(int width, int height, FreeRectChoiceHeuristic freeRectChoiceHeuristic, ref int score1, ref int score2 )
    {
        Rect newNode = Rect.zero;

        score1 = int.MaxValue;
        score2 = int.MaxValue;

        switch(freeRectChoiceHeuristic)
        {
            case FreeRectChoiceHeuristic.RectBestShortSideFit:
                newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
                break;
            case FreeRectChoiceHeuristic.RectBottomLeftRule:
                newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
                break;
            case FreeRectChoiceHeuristic.RectContactPointRule:
                newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                score1 = -score1; // Reverse since we are minimizing, but for contact point score bigger is better.
                break;
            case FreeRectChoiceHeuristic.RectBestLongSideFit:
                newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1);
                break;
            case FreeRectChoiceHeuristic.RectBestAreaFit:
                newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
                break;
        }

        if (newNode.height == 0)
        {
            score1 = int.MaxValue;
            score2 = int.MaxValue;
        }

        return newNode;
    }

    /// <summary>
    /// 放置一个rect到当前剩余的空间
    /// </summary>
    /// <param name="node"></param>
    private void PlaceRect(Rect node)
    {
        int numRectanglesToProcess = mFreeRectRangles.Count;

        for (int i=0; i<numRectanglesToProcess; ++i)
        {
            if (SplitFreeNode(mFreeRectRangles[i], node))
            {
                mFreeRectRangles.RemoveAt(i);

                --i;
                --numRectanglesToProcess;
            }
        }

        PruneFreeList();
        mUsedRectangles.Add(node);
    }

    /// <summary>
    /// 计算当前整个Rect的使用比例
    /// </summary>
    /// <returns></returns>
    private int Occupancy()
    {
        int usedSurfaceArea = 0;

        for (int i=0; i<mUsedRectangles.Count; ++i)
        {
            usedSurfaceArea += (int)(mUsedRectangles[i].width * mUsedRectangles[i].height);
        }

        return usedSurfaceArea / (mWidth * mHeight);
    }
}
