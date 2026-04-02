/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using FixMath.NET;

namespace Volatile
{
    public struct VoltVector2
    {
        public readonly Fix64 x;
        public readonly Fix64 y;

        public static VoltVector2 zero => new VoltVector2();

        public static Fix64 Dot(VoltVector2 a, VoltVector2 b)
        {
            return (a.x * b.x) + (a.y * b.y);
        }

        public Fix64 sqrMagnitude
        {
            get
            {
                return (this.x * this.x) + (this.y * this.y);
            }
        }

        public Fix64 magnitude
        {
            get
            {
                return VoltMath.Sqrt(this.sqrMagnitude);
            }
        }

        public VoltVector2 normalized
        {
            get
            {
                Fix64 magnitude = this.magnitude;
                return new VoltVector2(this.x / magnitude, this.y / magnitude);
            }
        }

        public VoltVector2(Fix64 x, Fix64 y)
        {
            this.x = x;
            this.y = y;
        }

        public static VoltVector2 operator *(VoltVector2 a, Fix64 b)
        {
            return new VoltVector2(a.x * b, a.y * b);
        }

        public static VoltVector2 operator *(Fix64 a, VoltVector2 b)
        {
            return new VoltVector2(b.x * a, b.y * a);
        }

        public static VoltVector2 operator +(VoltVector2 a, VoltVector2 b)
        {
            return new VoltVector2(a.x + b.x, a.y + b.y);
        }

        public static VoltVector2 operator -(VoltVector2 a, VoltVector2 b)
        {
            return new VoltVector2(a.x - b.x, a.y - b.y);
        }

        public static VoltVector2 operator -(VoltVector2 a)
        {
            return new VoltVector2(-a.x, -a.y);
        }


        public static Fix64 Distance(VoltVector2 a, VoltVector2 b)
        {
            Fix64 dx = b.x - a.x;
            Fix64 dy = b.y - a.y;
            return Fix64.Sqrt(dx * dx + dy * dy);
        }

        public static Fix64 DistanceSqr(VoltVector2 a, VoltVector2 b)
        {
            Fix64 dx = b.x - a.x;
            Fix64 dy = b.y - a.y;
            return dx * dx + dy * dy;
        }

        public static VoltVector2 Lerp(VoltVector2 a, VoltVector2 b, Fix64 t)
        {
            // Clamp t between 0 and 1
            if (t < Fix64.Zero) t = Fix64.Zero;
            if (t > Fix64.One) t = Fix64.One;
            
            return new VoltVector2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        public static VoltVector2 LerpUnclamped(VoltVector2 a, VoltVector2 b, Fix64 t)
        {
            return new VoltVector2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        public override string ToString()
        {
            return "{X:" + x + " Y:" + y + "}";
        }
    }
}