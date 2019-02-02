using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates 
{

[SerializeField]
    private int x, z;
    public int X
    {
        get { 
            return x;
        }
    }
    
    public int Z
    {
        get { 
            return z;
        }
    }


    public int Y {
        get{
            return -X - Z;
        } 
    }

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - Mathf.FloorToInt(z * 0.5f), z);
    }

    public override string ToString()
    {
        return string.Format("({0}, {1}, {2})", X, Y, Z);
    }

    public string ToStringOnSeparateLines()
    {
        return string.Format("x:{0}\ny:{1}\nz:{2}", X, Y, Z);
    }

    public static HexCoordinates FromPosition(Vector3 pos)
    {
        /**
        行  样式   原本下标         偏移下标    偏移的外径个数
        6   x       0      ->         0     6
        5    x       0     ->        0      5
        4   x       0      ->       0       4
        3    x       0     ->      0        3
        2   x       0      ->     0         2
        1    x       0     ->    0          1
        0   x       0      ->   0           0
        这里计算出来的x值是 在错行的时候是有问题的
        比方这样的时候 点击行1的左半边和右半边得出来的值是不一样的 因为这个值计算的是纯粹垂直的算法
         */
        float x = pos.x / (HexMetrics.innerRadius * 2);
        float y = -x;
        // 这个偏移量 就是每行偏移多少
        float offset = pos.z / (HexMetrics.outerRadius * 3);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        // 当距离中心越远的时候 iX + iY + iZ != 0 取整错误
        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x -y -iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            } 
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }
        return new HexCoordinates(iX, iZ);
    }

}
