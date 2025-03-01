using System;

namespace Game.Scripts
{
    [Serializable]
    public class OptimizedMatrix
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;

        public OptimizedMatrix(Matrix matrix)
        {
            m00 = matrix.m00;
            m01 = matrix.m01;
            m02 = matrix.m02;
            m03 = matrix.m03;

            m10 = matrix.m10;
            m11 = matrix.m11;
            m12 = matrix.m12;
            m13 = matrix.m13;

            m20 = matrix.m20;
            m21 = matrix.m21;
            m22 = matrix.m22;
            m23 = matrix.m23;
        }
    }

}