using System.Runtime.CompilerServices;
using Unity.Mathematics;
using m = Unity.Mathematics.math;

public struct gmath
{
    private static float3 forward = new float3(0, 0, 1);
    private static float3 up = new float3(0, 1, 0);
    private static float3 right = new float3(1, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Float2ToHeading(float2 dir)
    {
        return ToAngleRange360(m.degrees(m.atan2(dir.x, dir.y)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 HeadingToFloat2(float heading)
    {
        heading = m.radians(heading);
        return new float2(m.sin(heading), m.cos(heading));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float QuaternionToHeading(quaternion q)
    {
        float4 v = q.value;
        float siny_cosp = 2.0f * (v.w * v.z + v.x * v.y);
        float cosy_cosp = 1.0f - 2.0f * (v.y * v.y + v.z * v.z);
        return -m.degrees(m.atan2(siny_cosp, cosy_cosp));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SignedInnerAngle(float from, float to)
    {
        float res = to - from;
        res = (MathMod((res + 180f), 360f)) - 180f;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Magnitude(float2 v)
    {
        return m.sqrt(v.x * v.x + v.y * v.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToAngleRange360(float a)
    {
        return (a + 360f) % 360f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float MathMod(float x, float n)
    {
        return x - m.floor(x / n) * n;
    }

    /// <summary>
    /// Returns int in the range 0-127 inclusive.
    /// </summary>
    public static int FastNoise(float x, float y)
    {
        const int frequence256 = 256;
        int sx = (int)((x) * frequence256);
        int sy = (int)((y) * frequence256);

        int bX = sx & 0xFF;
        int bY = sy & 0xFF;

        int sxp = sx >> 8;
        int syp = sy >> 8;

        //Compute noise for each corner of current cell
        int Y1376312589_00 = syp * 1376312589;
        int Y1376312589_01 = Y1376312589_00 + 1376312589;

        int XY1376312589_00 = sxp + Y1376312589_00;
        int XY1376312589_10 = XY1376312589_00 + 1;
        int XY1376312589_01 = sxp + Y1376312589_01;
        int XY1376312589_11 = XY1376312589_01 + 1;

        int XYBASE_00 = (XY1376312589_00 << 13) ^ XY1376312589_00;
        int XYBASE_10 = (XY1376312589_10 << 13) ^ XY1376312589_10;
        int XYBASE_01 = (XY1376312589_01 << 13) ^ XY1376312589_01;
        int XYBASE_11 = (XY1376312589_11 << 13) ^ XY1376312589_11;

        int alt1 = (XYBASE_00 * (XYBASE_00 * XYBASE_00 * 15731 + 789221) + 1376312589);
        int alt2 = (XYBASE_10 * (XYBASE_10 * XYBASE_10 * 15731 + 789221) + 1376312589);
        int alt3 = (XYBASE_01 * (XYBASE_01 * XYBASE_01 * 15731 + 789221) + 1376312589);
        int alt4 = (XYBASE_11 * (XYBASE_11 * XYBASE_11 * 15731 + 789221) + 1376312589);

        // value noise
        alt1 &= 0xFFFF;
        alt2 &= 0xFFFF;
        alt3 &= 0xFFFF;
        alt4 &= 0xFFFF;

        //BiLinear interpolation 
        int f24 = (bX * bY) >> 8;
        int f23 = bX - f24;
        int f14 = bY - f24;
        int f13 = 256 - f14 - f23 - f24;

        int val = (alt1 * f13 + alt2 * f23 + alt3 * f14 + alt4 * f24);
        int result = (val << 1);
        return (int)((uint)result >> 18);
    }

    /// <summary>
    /// Single octave returns int in range 0-127. Additional octaves increase range towards 255.
    /// </summary>
    public static int FastNoise(float x, float y, int nbOctave)
    {
        int result = 0;
        int frequence256 = 256;
        int sx = (int)((x) * frequence256);
        int sy = (int)((y) * frequence256);
        int octave = nbOctave;
        while (octave != 0)
        {
            int bX = sx & 0xFF;
            int bY = sy & 0xFF;

            int sxp = sx >> 8;
            int syp = sy >> 8;


            //Compute noise for each corner of current cell
            int Y1376312589_00 = syp * 1376312589;
            int Y1376312589_01 = Y1376312589_00 + 1376312589;

            int XY1376312589_00 = sxp + Y1376312589_00;
            int XY1376312589_10 = XY1376312589_00 + 1;
            int XY1376312589_01 = sxp + Y1376312589_01;
            int XY1376312589_11 = XY1376312589_01 + 1;

            int XYBASE_00 = (XY1376312589_00 << 13) ^ XY1376312589_00;
            int XYBASE_10 = (XY1376312589_10 << 13) ^ XY1376312589_10;
            int XYBASE_01 = (XY1376312589_01 << 13) ^ XY1376312589_01;
            int XYBASE_11 = (XY1376312589_11 << 13) ^ XY1376312589_11;

            int alt1 = (XYBASE_00 * (XYBASE_00 * XYBASE_00 * 15731 + 789221) + 1376312589);
            int alt2 = (XYBASE_10 * (XYBASE_10 * XYBASE_10 * 15731 + 789221) + 1376312589);
            int alt3 = (XYBASE_01 * (XYBASE_01 * XYBASE_01 * 15731 + 789221) + 1376312589);
            int alt4 = (XYBASE_11 * (XYBASE_11 * XYBASE_11 * 15731 + 789221) + 1376312589);

            /*
             *NOTE : on  for true grandiant noise uncomment following block
             * for true gradiant we need to perform scalar product here, gradiant vector are created/deducted using
             * the above pseudo random values (alt1...alt4) : by cutting thoses values in twice values to get for each a fixed x,y vector 
             * gradX1= alt1&0xFF 
             * gradY1= (alt1&0xFF00)>>8
             *
             * the last part of the PRN (alt1&0xFF0000)>>8 is used as an offset to correct one of the gradiant problem wich is zero on cell edge
             *
             * source vector (sXN;sYN) for scalar product are computed using (bX,bY)
             *
             * each four values  must be replaced by the result of the following 
             * altN=(gradXN;gradYN) scalar (sXN;sYN)
             *
             * all the rest of the code (interpolation+accumulation) is identical for value & gradiant noise
             */


            #region true gradiant noise

            //int grad1X = (alt1 & 0xFF) - 128;
            //int grad1Y = ((alt1 >> 8) & 0xFF) - 128;
            //int grad2X = (alt2 & 0xFF) - 128;
            //int grad2Y = ((alt2 >> 8) & 0xFF) - 128;
            //int grad3X = (alt3 & 0xFF) - 128;
            //int grad3Y = ((alt3 >> 8) & 0xFF) - 128;
            //int grad4X = (alt4 & 0xFF) - 128;
            //int grad4Y = ((alt4 >> 8) & 0xFF) - 128;


            //int sX1 = bX >> 1;
            //int sY1 = bY >> 1;
            //int sX2 = 128 - sX1;
            //int sY2 = sY1;
            //int sX3 = sX1;
            //int sY3 = 128 - sY1;
            //int sX4 = 128 - sX1;
            //int sY4 = 128 - sY1;
            //alt1 = (grad1X * sX1 + grad1Y * sY1) + 16384 + ((alt1 & 0xFF0000) >> 9); //to avoid seams to be 0 we use an offset
            //alt2 = (grad2X * sX2 + grad2Y * sY2) + 16384 + ((alt2 & 0xFF0000) >> 9);
            //alt3 = (grad3X * sX3 + grad3Y * sY3) + 16384 + ((alt3 & 0xFF0000) >> 9);
            //alt4 = (grad4X * sX4 + grad4Y * sY4) + 16384 + ((alt4 & 0xFF0000) >> 9);

            #endregion

            #region value noise


            alt1 &= 0xFFFF;
            alt2 &= 0xFFFF;
            alt3 &= 0xFFFF;
            alt4 &= 0xFFFF;

            #endregion

            #region linear interpolation

            //BiLinear interpolation 

            int f24 = (bX * bY) >> 8;
            int f23 = bX - f24;
            int f14 = bY - f24;
            int f13 = 256 - f14 - f23 - f24;

            int val = (alt1 * f13 + alt2 * f23 + alt3 * f14 + alt4 * f24);


            #endregion

            #region bicubic interpolation

            //BiCubic interpolation ( in the form alt(bX) = alt[n] - (3*bX^2 - 2*bX^3) * (alt[n] - alt[n+1]) )
            //int bX2 = (bX * bX) >> 8;
            //int bX3 = (bX2 * bX) >> 8;
            //int _3bX2 = 3 * bX2;
            //int _2bX3 = 2 * bX3;
            //int alt12 = alt1 - (((_3bX2 - _2bX3) * (alt1 - alt2)) >> 8);
            //int alt34 = alt3 - (((_3bX2 - _2bX3) * (alt3 - alt4)) >> 8);


            //int bY2 = (bY * bY) >> 8;
            //int bY3 = (bY2 * bY) >> 8;
            //int _3bY2 = 3 * bY2;
            //int _2bY3 = 2 * bY3;
            //int val = alt12 - (((_3bY2 - _2bY3) * (alt12 - alt34)) >> 8);

            //val *= 256;

            #endregion

            //Accumulate in result
            result += (val << octave);

            octave--;
            sx <<= 1;
            sy <<= 1;

        }

        return (int)((uint)result >> (16 + nbOctave + 1));
    }
}