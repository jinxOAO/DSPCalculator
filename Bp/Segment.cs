using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.Bp
{
    public class Segment
    {
        public Vector2 p1;
        public Vector2 p2;
        public float k;
        public float b;
        public bool isVert;
        public Vector2 vec;

        public Segment(float x1, float y1, float x2, float y2)
        {
            p1 = new Vector2(x1, y1);
            p2 = new Vector2(x2, y2);
            if (Math.Abs(x1 - x2) >= 0.001f)
            {
                k = (y1 - y2) / (x1 - x2);
            }
            else
            {
                isVert = true; // k为无穷
                k = 0;
            }
            b = y1 - k * x1;
            vec = new Vector2(x2 - x1, y2 - y1);
        }

        /// <summary>
        /// 判断两个线段是不是相交或者离得过近（最近距离小于minDistance）
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        public bool CrossOrNear(Segment other, float minDistance)
        {
            float squaredDistance = minDistance * minDistance;

            // 首先判断相交，不相交则判断距离
            if (isVert && other.isVert) // 如果平行，且都是k为无穷的情况，直接判断x距离
            {
                return Math.Abs(p1.x - other.p1.x) < minDistance;
            }
            else if (Math.Abs(other.k - k) <= 0.0001f && isVert == other.isVert) // 如果平行，直接判断距离
            {
                return ((other.b - b) * (other.b - b) / (1 + k * k)) < squaredDistance; // 如果距离够远则不near
            }
            else // 不平行
            {
                Vector2 v1 = new Vector2(other.p1.x - p1.x, other.p1.y - p1.y);
                Vector2 v2 = new Vector2(other.p2.x - p1.x, other.p2.y - p1.y);
                float c1 = vec.Cross(v1);
                float c2 = vec.Cross(v2);
                float res1 = c1 * c2;

                Vector2 vv1 = new Vector2(p1.x - other.p1.x, p1.y - other.p1.y);
                Vector2 vv2 = new Vector2(p2.x - other.p1.x, p2.y - other.p1.y);
                float cc1 = other.vec.Cross(vv1);
                float cc2 = other.vec.Cross(vv2);
                float res2 = cc1 * cc2;
                if (res1 <= 0 && res2 <= 0)
                {
                    return true;
                }
                else if (res1 <= 0) // 到这里，说明没相交，且this指向other线段内（this的延长线与other线段相交）
                {
                    // 求this的两个端点到other所在直线的最小距离
                    if(other.isVert)
                    {
                        return Math.Min(Math.Abs(p1.x - other.p1.x), Math.Abs(p2.x - other.p1.x)) < minDistance;
                    }
                    return Math.Min(p1.DistanceSquare(other.k, other.b), p2.DistanceSquare(other.k, other.b)) < squaredDistance;
                }
                else if (res2 <= 0) // 没相交，且other指向this线段内（other的延长线与this线段相交）
                {
                    // 求other的两个端点到this所在直线的最小距离
                    if(isVert)
                    {
                        return Math.Min(Math.Abs(p1.x - other.p1.x), Math.Abs(p1.x - other.p2.x)) < minDistance;
                    }
                    return Math.Min(other.p1.DistanceSquare(k, b), other.p2.DistanceSquare(k, b)) < squaredDistance;
                }
                else // res都大于0，代表互相指向线段外（所在直线的交点都不在线段上）
                {
                    float dis1;
                    if (other.isVert)
                    {
                        dis1 = Math.Min(Math.Abs(p1.x - other.p1.x), Math.Abs(p2.x - other.p1.x));
                        dis1 = dis1 * dis1;
                    }
                    else
                        dis1 = Math.Min(p1.DistanceSquare(other.k, other.b), p2.DistanceSquare(other.k, other.b));
                    float dis2;
                    if (isVert)
                    {
                        dis2 = Math.Min(Math.Abs(p1.x - other.p1.x), Math.Abs(p1.x - other.p2.x));
                        dis2 = dis2 * dis2;
                    }
                    else
                        dis2 = Math.Min(other.p1.DistanceSquare(k, b), other.p2.DistanceSquare(k, b));
                    return Math.Max(dis1, dis2) < squaredDistance;
                    //// 求this每个端点到other每个端点，最短距离小于要求距离即视为过近
                    //float d1 = (other.p1.x - p1.x).Square() + (other.p1.y - p1.y).Square();
                    //if (d1 < squaredDistance)
                    //    return true;
                    //float d2 = (other.p1.x - p2.x).Square() + (other.p1.y - p2.y).Square();
                    //if (d2 < squaredDistance)
                    //    return true;
                    //float d3 = (other.p2.x - p1.x).Square() + (other.p2.y - p1.y).Square();
                    //if (d3 < squaredDistance)
                    //    return true;
                    //float d4 = (other.p2.x - p2.x).Square() + (other.p2.y - p2.y).Square();
                    //if (d4 < squaredDistance)
                    //    return true;
                }
            }
            return false;
        }
    }
}
