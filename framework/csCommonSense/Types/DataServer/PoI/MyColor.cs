using System.Windows.Media;
using ProtoBuf;

namespace DataServer
{
    public struct MyColor
    {
        [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
        public uint ARGB { get; set; }

        public static explicit operator MyColor(Color color)
        {
            return new MyColor { ARGB = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B };
        }
        public static explicit operator Color(MyColor color)
        {
            return Color.FromArgb((byte)(color.ARGB >> 24), (byte)(color.ARGB >> 16), (byte)(color.ARGB >> 8), (byte)color.ARGB);
        }
    }
}