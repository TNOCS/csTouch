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
using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.Design.Serialization;
using System.Security;

namespace DevZest.Windows
{
    /// <summary>Converts instances of other types to and from <see cref="SplitterDistance"/> instances.</summary>
    /// <remarks><see cref="SplitterDistanceConverter"/> supports conversion to and from the following types: String, Decimal, Single, Double, Int16, Int32, Int64, UInt16, UInt32, UInt64.</remarks>
    public class SplitterDistanceConverter : TypeConverter
    {
        private static readonly string[] UnitTypeStrings = new string[] { "PX", "*" };
        private static readonly SplitterUnitType[] UnitTypes = new SplitterUnitType[] { SplitterUnitType.Pixel, SplitterUnitType.Star };
        private static readonly string[] PixelUnitStrings = new string[] { "in", "cm", "pt" };
        private static readonly double[] PixelUnitFactors = new double[] { 96.0, 37.795275590551178, 1.3333333333333333 };

        /// <exclude/>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            switch (Type.GetTypeCode(sourceType))
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.String:
                    return true;
            }
            return false;
        }

        /// <exclude/>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType != typeof(InstanceDescriptor))
                return (destinationType == typeof(string));
            return true;
        }

        /// <exclude/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                throw base.GetConvertFromException(value);
            string str = value as string;
            if (str != null)
                return FromString(str, culture);

            SplitterUnitType unitType;
            double num = Convert.ToDouble(value, culture);
            unitType = SplitterUnitType.Pixel;

            return new SplitterDistance(num, unitType);
        }

        /// <exclude/>
        [SecurityCritical]
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");
            if ((value != null) && (value is SplitterDistance))
            {
                SplitterDistance distance = (SplitterDistance)value;
                if (destinationType == typeof(string))
                    return ToString(distance, culture);
                if (destinationType == typeof(InstanceDescriptor))
                    return new InstanceDescriptor(typeof(SplitterDistance).GetConstructor(new Type[] { typeof(double), typeof(SplitterUnitType) }), new object[] { distance.Value, distance.UnitType });
            }
            throw base.GetConvertToException(value, destinationType);
        }

        internal static SplitterDistance FromString(string s, CultureInfo cultureInfo)
        {
            double num;
            SplitterUnitType type;
            FromString(s, cultureInfo, out num, out type);
            return new SplitterDistance(num, type);
        }

        private static void FromString(string s, CultureInfo cultureInfo, out double value, out SplitterUnitType unit)
        {
            string str = s.Trim().ToUpperInvariant();
            if (str == "AUTO")
            {
                value = double.NaN;
                unit = SplitterUnitType.Pixel;
                return;
            }

            if (str == "AUTO*")
            {
                value = double.NaN;
                unit = SplitterUnitType.Star;
                return;
            }

            value = 0.0;
            unit = SplitterUnitType.Pixel;
            int length = str.Length;
            int suffixLength = 0;
            double factor = 1.0;
            for (int i = 0; i < UnitTypeStrings.Length; i ++)
            {
                if (str.EndsWith(UnitTypeStrings[i], StringComparison.Ordinal))
                {
                    suffixLength = UnitTypeStrings[i].Length;
                    unit = UnitTypes[i];
                    break;
                }
            }

            if (suffixLength == 0)
            {
                for (int i = 0; i < PixelUnitStrings.Length; i++)
                {
                    if (str.EndsWith(PixelUnitStrings[i], StringComparison.Ordinal))
                    {
                        suffixLength = PixelUnitStrings[i].Length;
                        factor = PixelUnitFactors[i];
                        break;
                    }
                }
            }

            if (length == suffixLength && unit == SplitterUnitType.Star)
                value = 1.0;
            else
            {
                string str2 = str.Substring(0, length - suffixLength);
                value = Convert.ToDouble(str2, cultureInfo) * factor;
            }
        }

        internal static string ToString(SplitterDistance distance, CultureInfo cultureInfo)
        {
            if (distance.IsAutoPixel)
                return "Auto";
            else if (distance.IsAutoStar)
                return "Auto*";
            else if (distance.UnitType == SplitterUnitType.Star)
            {
                if (DoubleUtil.IsOne(distance.Value))
                    return "*";
                else
                    return (Convert.ToString(distance.Value, cultureInfo) + "*");
            }
            else
                return Convert.ToString(distance.Value, cultureInfo);
        }
    }
}
