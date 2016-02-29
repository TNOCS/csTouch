/***********************************************************************
Copyright DevZest, 2009 (http://www.devzest.com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

This program is part of WPF Docking, a commercial undo/redo-able
docking library, which you can download from http://www.devzest.com.
You can obtain a Free Feature License of WPF Docking through
installed License Console, FREE OF CHARGE, with the benefit of writing
proprietary software, along with commercial product quality
documentation, upgrade and free technical support.
**********************************************************************/

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Runtime.InteropServices;

namespace DevZest.Windows
{
    internal static class DoubleUtil
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct NanUnion
        {
            [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
            [FieldOffset(0)]
            internal double DoubleValue;
            [FieldOffset(0)]
            internal ulong UintValue;
        }

        public static bool IsNaN(double value)
        {
            NanUnion union = new NanUnion();
            union.DoubleValue = value;
            ulong num = union.UintValue & 18442240474082181120L;
            ulong num2 = union.UintValue & ((ulong)0xfffffffffffffL);
            if ((num != 0x7ff0000000000000L) && (num != 18442240474082181120L))
            {
                return false;
            }
            return (num2 != 0L);
        }

        public static bool IsOne(double value)
        {
            return (Math.Abs((double)(value - 1.0)) < 2.2204460492503131E-15);
        }

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
                return true;
            double num = ((Math.Abs(value1) + Math.Abs(value2)) + 10.0) * 2.2204460492503131E-16;
            double num2 = value1 - value2;
            return ((-num < num2) && (num > num2));
        }

        public static bool AreClose(Point point1, Point point2)
        {
            return (AreClose(point1.X, point2.X) && AreClose(point1.Y, point2.Y));
        }
    }
}
