using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaxRectsBinPack {

    /// <summary>
    /// 允许翻转
    /// </summary>
    public bool bAllowFilp;

    /// <summary>
    /// 宽度
    /// </summary>
    public float mWidth;

    /// <summary>
    /// 高度
    /// </summary>
    public float mHeight;

    /// <summary>
    /// 已经使用的Rect
    /// </summary>
    private List<Rect> mUsedRectangles;

    /// <summary>
    /// 还未使用的Rect
    /// </summary>
    private List<Rect> mFreeRectRangles;


    public enum FreeRectChoiceHeuristic
    {
        RectBestShortSideFit, ///< -BSSF: Positions the rectangle against the short side of a free rectangle into which it fits the best.
		RectBestLongSideFit, ///< -BLSF: Positions the rectangle against the long side of a free rectangle into which it fits the best.
		RectBestAreaFit, ///< -BAF: Positions the rectangle into the smallest free rect into which it fits.
		RectBottomLeftRule, ///< -BL: Does the Tetris placement.
		RectContactPointRule ///< -CP: Choosest the placement where the rectangle touches other rects as much as possible.
	};

    public MaxRectsBinPack(float width, float height, bool allowFilp)
    {
        bAllowFilp = allowFilp;
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
    public Rect Insert(float width, float height, FreeRectChoiceHeuristic freeRectChoiceHeuristic)
    {
        Rect newNode = Rect.zero;

        // Unused in this function. We don't need to know the score after finding the position.
        float score1 = float.MaxValue;
        float score2 = float.MaxValue;

        switch (freeRectChoiceHeuristic)
        {
            case FreeRectChoiceHeuristic.RectBestShortSideFit:
                 newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2);
                 break;
            case FreeRectChoiceHeuristic.RectBestLongSideFit:
                 newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2);
                 break;
            case FreeRectChoiceHeuristic.RectBestAreaFit:
                newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
                break;
            case FreeRectChoiceHeuristic.RectBottomLeftRule:
                newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score1, ref score2);
                break;
            case FreeRectChoiceHeuristic.RectContactPointRule:
                newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2);
                break;
        }

        if (newNode.height == 0)
            return newNode;

        float numRectanglesToProcess = mFreeRectRangles.Count;

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
            float bestScore1 = float.MaxValue;
            float bestScore2 = float.MaxValue;

            int bestRectIndex = -1;

            Rect bestNode = Rect.zero;

            for (int i=0; i<rects.Count; ++i)
            {
                float score1 = float.MaxValue;
                float score2 = float.MaxValue;

                Rect newNode = ScoreRect((float)rects[i].x, (float)rects[i].y, freeRectChoiceHeuristic, ref score1, ref score2);

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

    /// <summary>
    /// 找到一块 短边(取width, height其中短的) 填充率最高的剩余空间。
    /// </summary>
    /// <returns></returns>
    public Rect FindPositionForNewNodeBestShortSideFit(float width, float height, ref float bestShortSideFit, ref float bestLongSideFit)
    {
        Rect bestNode = Rect.zero;

        bestShortSideFit = float.MaxValue;
        bestLongSideFit = float.MaxValue;

        for (int i=0; i < mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                float leftoverHoriz = Mathf.Abs(mFreeRectRangles[i].width - width);
                float leftoverVert = Mathf.Abs(mFreeRectRangles[i].height - height);

                float shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                float longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

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

            if (bAllowFilp && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                float flippedLeftoverHoriz = Mathf.Abs(mFreeRectRangles[i].width - height);
                float flippedLeftoverVert = Mathf.Abs(mFreeRectRangles[i].height - width);

                float flippedShortSideFit = Mathf.Min(flippedLeftoverHoriz, flippedLeftoverVert);
                float flippedLongSideFit = Mathf.Max(flippedLeftoverHoriz, flippedLeftoverVert);

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
    public Rect FindPositionForNewNodeBestLongSideFit(float width, float height, ref float bestShortSideFit, ref float bestLongSideFit)
    {
        Rect bestNode = Rect.zero;

        bestShortSideFit = float.MaxValue;
        bestLongSideFit = float.MaxValue;

        for (int i=0; i<mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                float leftoverHoriz = Mathf.Abs(mFreeRectRangles[i].width - width);
                float leftoverVert = Mathf.Abs(mFreeRectRangles[i].height - height);

                float shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                float longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

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

            if (bAllowFilp && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                float leftoverHoriz = Mathf.Abs(mFreeRectRangles[i].width - height);
                float leftoverVert = Mathf.Abs(mFreeRectRangles[i].height - width);

                float shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                float longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

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
    public Rect FindPositionForNewNodeBottomLeft(float width, float height, ref float bestX, ref float bestY)
    {
        Rect bestNode = Rect.zero;

        bestY = float.MaxValue;
        bestX = float.MaxValue;

        for (int i=0; i<mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                float topSideY = mFreeRectRangles[i].y + height;
                if (topSideY < bestY || topSideY == bestY && mFreeRectRangles[i].x < bestX)
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestY = topSideY;
                    bestX = mFreeRectRangles[i].x;
                }
            }

            if (bAllowFilp && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                float topSideY = mFreeRectRangles[i].y + width;
                if (topSideY < bestY || (topSideY == bestY && mFreeRectRangles[i].x < bestX))
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = height;
                    bestNode.height = width;

                    bestY = topSideY;
                    bestX = mFreeRectRangles[i].x;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    /// 根据长，宽的比例相加结果为因子来计算最优
    /// </summary>
    public Rect FindPositionForNewNodeContactPoint(float width, float height, ref float bestContactScore)
    {
        Rect bestNode = Rect.zero;

        bestContactScore = -1f;

        for (int i=0; i< mFreeRectRangles.Count; ++i)
        {
            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                float score = ContactPointScoreNode(mFreeRectRangles[i].x, mFreeRectRangles[i].y, width, height);
                if (score > bestContactScore)
                {
                    bestNode.x = mFreeRectRangles[i].x;
                    bestNode.y = mFreeRectRangles[i].y;

                    bestNode.width = width;
                    bestNode.height = height;

                    bestContactScore = score;
                }
            }

            if (mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                float score = ContactPointScoreNode(mFreeRectRangles[i].x, mFreeRectRangles[i].y, height, width);

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
    public Rect FindPositionForNewNodeBestAreaFit(float width, float height, ref float bestAreaFit, ref float bestShortSideFit)
    {
        Rect bestNode = Rect.zero;

        bestAreaFit = float.MaxValue;
        bestShortSideFit = float.MaxValue;

        for (int i=0; i < mFreeRectRangles.Count; ++i)
        {
            float areaFit = mFreeRectRangles[i].width * mFreeRectRangles[i].height - width * height;

            if (mFreeRectRangles[i].width >= width && mFreeRectRangles[i].height >= height)
            {
                float leftoverHoriz = Mathf.Abs(mFreeRectRangles[i].width - width);
                float leftoverVert = Mathf.Abs(mFreeRectRangles[i].height - height);

                float shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);

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

            if (bAllowFilp && mFreeRectRangles[i].width >= height && mFreeRectRangles[i].height >= width)
            {
                float leftoverHoriz = Mathf.Abs(mFreeRectRangles[i].width - height);
                float leftoverVert = Mathf.Abs(mFreeRectRangles[i].height - width);
                float shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);

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

    private float ContactPointScoreNode(float x, float y, float width, float height)
    {
        float score = 0;

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
                score += CommonIntervalLength(mUsedRectangles[i].y, mUsedRectangles[i].y + mUsedRectangles[i].height, y, y + height);
            }

            if (mUsedRectangles[i].y == y + height || mUsedRectangles[i].y + mUsedRectangles[i].height == y)
            {
                score += CommonIntervalLength(mUsedRectangles[i].x, mUsedRectangles[i].x + mUsedRectangles[i].width, x, x + height);
            }
        }

        return score;
    }

    private float CommonIntervalLength(float i1start, float i1end, float i2start, float i2end)
    {
        if (i1end < i2start || i2end < i1start)
            return 0;

        return Mathf.Min(i1end, i2end) - Mathf.Max(i1start, i2start);
    }

    private bool SplitFreeNode(Rect freeNode, Rect usedNode)
    {
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
    private Rect ScoreRect(float width, float height, FreeRectChoiceHeuristic freeRectChoiceHeuristic, ref float score1, ref float score2 )
    {
        Rect newNode = Rect.zero;

        score1 = float.MaxValue;
        score2 = float.MaxValue;

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
            score1 = float.MaxValue;
            score2 = float.MaxValue;
        }

        return newNode;
    }

    /// <summary>
    /// 放置一个rect到当前剩余的空间
    /// </summary>
    /// <param name="node"></param>
    private void PlaceRect(Rect node)
    {
        float numRectanglesToProcess = mFreeRectRangles.Count;

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
    private float Occupancy()
    {
        float usedSurfaceArea = 0;

        for (int i=0; i<mUsedRectangles.Count; ++i)
        {
            usedSurfaceArea += mUsedRectangles[i].width * mUsedRectangles[i].height;
        }

        return usedSurfaceArea / (mWidth * mHeight);
    }
}
