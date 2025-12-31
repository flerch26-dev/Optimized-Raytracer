using UnityEngine;

public struct Matrix3x3
{
    public float m00, m01, m02;
    public float m10, m11, m12;
    public float m20, m21, m22;

    // Constructor
    public Matrix3x3(
        float m00, float m01, float m02,
        float m10, float m11, float m12,
        float m20, float m21, float m22)
    {
        this.m00 = m00; this.m01 = m01; this.m02 = m02;
        this.m10 = m10; this.m11 = m11; this.m12 = m12;
        this.m20 = m20; this.m21 = m21; this.m22 = m22;
    }

    // Optional: Access by index
    public float this[int row, int col]
    {
        get
        {
            return row switch
            {
                0 => col switch { 0 => m00, 1 => m01, 2 => m02, _ => throw new System.IndexOutOfRangeException() },
                1 => col switch { 0 => m10, 1 => m11, 2 => m12, _ => throw new System.IndexOutOfRangeException() },
                2 => col switch { 0 => m20, 1 => m21, 2 => m22, _ => throw new System.IndexOutOfRangeException() },
                _ => throw new System.IndexOutOfRangeException()
            };
        }
        set
        {
            switch (row)
            {
                case 0:
                    switch (col)
                    {
                        case 0: m00 = value; break;
                        case 1: m01 = value; break;
                        case 2: m02 = value; break;
                    }
                    break;
                case 1:
                    switch (col)
                    {
                        case 0: m10 = value; break;
                        case 1: m11 = value; break;
                        case 2: m12 = value; break;
                    }
                    break;
                case 2:
                    switch (col)
                    {
                        case 0: m20 = value; break;
                        case 1: m21 = value; break;
                        case 2: m22 = value; break;
                    }
                    break;
            }
        }
    }

    // Optional: Multiply by Vector3
    public Vector3 Multiply(Vector3 v)
    {
        return new Vector3(
            m00 * v.x + m01 * v.y + m02 * v.z,
            m10 * v.x + m11 * v.y + m12 * v.z,
            m20 * v.x + m21 * v.y + m22 * v.z
        );
    }

    public static Matrix3x3 Identity => new Matrix3x3(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1
    );
}


