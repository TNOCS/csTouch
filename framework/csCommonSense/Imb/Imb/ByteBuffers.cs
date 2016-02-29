using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMB3.ByteBuffers
{
    public class TByteBuffer
    {
        // constructors
        public TByteBuffer(int aLength = 0) { Clear(aLength); }
        public TByteBuffer(byte[] aBuffer, int aLength = 0) 
        {
            if (aLength == 0)
            {
                FReadCursor = 0;
                FWriteCursor = 0;
                FPrepareCursor = 0;
                FBuffer = aBuffer;
            }
            else
            {
                Clear(aLength);
                Array.Copy(aBuffer, FBuffer, aLength);
            }
        }
        // private
        private byte[] FBuffer = new byte[0];
        private int FReadCursor;
        private int FWriteCursor;
        private int FPrepareCursor;
        private int FPBWriteCursor;
        // public
        public int Length 
        { 
            get { return FBuffer.Length; }
            set { if (value != FBuffer.Length) Array.Resize(ref FBuffer, value); } 
        }
        public byte[] Buffer 
        { 
            get { return FBuffer; } 
            set { FBuffer = value; FReadCursor = 0; FWriteCursor = 0; FPrepareCursor = 0; FPBWriteCursor = 0; } 
        }
        public void Clear(int aLength = 0) 
        { 
            Length = aLength; 
            FReadCursor = 0; 
            FWriteCursor = 0; 
            FPrepareCursor = 0; 
            FPBWriteCursor = 0; 
        }
        public bool IsEmpty { get { return (FBuffer == null || FBuffer.Length == 0); } }
        public int ReadCursor { get { return FReadCursor; } }
        public int WriteCursor { get { return FWriteCursor; } }
        public int PBWriteCursor { get { return FPBWriteCursor; } }
        // reading, preparing and writing
        public void ReadStart() { FReadCursor = 0; }
        public int ReadAvailable { get { return Length - FReadCursor; } }
        // read
        public bool Read(out bool aValue)
        {
            if (sizeof(bool) <= ReadAvailable)
            {
                aValue = BitConverter.ToBoolean(FBuffer, FReadCursor);
                FReadCursor += sizeof(bool);
                return true;
            }
            else
            {
                aValue = false;
                return false;
            }
        }
        public bool Read(out byte aValue)
        {
            if (sizeof(byte) <= ReadAvailable)
            {
                aValue = FBuffer[FReadCursor];
                FReadCursor++;
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Read(out Int32 aValue)
        {
            if (sizeof(Int32) <= ReadAvailable)
            {
                aValue = BitConverter.ToInt32(FBuffer, FReadCursor);
                FReadCursor += sizeof(Int32);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Read(out Int64 aValue)
        {
            if (sizeof(Int64) <= ReadAvailable)
            {
                aValue = BitConverter.ToInt64(FBuffer, FReadCursor);
                FReadCursor += sizeof(Int64);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Read(out Single aValue)
        {
            if (sizeof(Single) <= ReadAvailable)
            {
                aValue = BitConverter.ToSingle(FBuffer, FReadCursor);
                FReadCursor += sizeof(Single);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Read(out Double aValue)
        {
            if (sizeof(Double) <= ReadAvailable)
            {
                aValue = BitConverter.ToDouble(FBuffer, FReadCursor);
                FReadCursor += sizeof(Double);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Read(out string aValue)
        {
            Int32 Len;
            if (Read(out Len))
            {
                if (Len <= ReadAvailable)
                {
                    aValue = Encoding.UTF8.GetString(FBuffer, FReadCursor, Len);
                    FReadCursor += Len;
                    return true;
                }
                else
                {
                    aValue = "";
                    return false;
                }
            }
            else
            {
                aValue = "";
                return false;
            }
        }
        // read aValue.Length number of bytes if available
        public bool Read(ref byte[] aValue)
        {
            if (aValue.Length <= ReadAvailable)
            {
                Array.Copy(FBuffer, FReadCursor, aValue, 0, aValue.Length);
                FReadCursor += aValue.Length;
                return true;
            }
            else
                return false;
        }
        // read size and data and store WITH size and data
        public bool Read(TByteBuffer aValue)
        {
            Int32 Len;
            if (Read(out Len))
            {
                if (Len <= ReadAvailable)
                {
                    aValue.PrepareSize(Len + sizeof(Int32));
                    aValue.PrepareApply();
                    aValue.QWrite(Len);
                    Array.Copy(FBuffer, FReadCursor, aValue.FBuffer, aValue.FWriteCursor, Len);
                    FReadCursor += Len;
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        // read type result
        // todo: check postfix ++
        public bool ReadBoolean(bool aDefaultValue = false)
        {
            if (sizeof(bool) <= ReadAvailable)
                return BitConverter.ToBoolean(FBuffer, FReadCursor++);
            else
                return aDefaultValue;
        }
        // todo: check postfix ++
        public byte ReadByte(byte aDefaultValue = 0)
        {
            if (sizeof(byte) <= ReadAvailable)
                return FBuffer[FReadCursor++];
            else
                return aDefaultValue;
        }
        public Int32 ReadInt32(Int32 aDefaultValue = 0)
        {
            if (sizeof(Int32) <= ReadAvailable)
            {
                FReadCursor += sizeof(Int32);
                return BitConverter.ToInt32(FBuffer, FReadCursor - sizeof(Int32));
            }
            else
                return aDefaultValue;
        }
        public Int64 ReadInt64(Int64 aDefaultValue = 0)
        {
            if (sizeof(Int64) <= ReadAvailable)
            {
                FReadCursor += sizeof(Int64);
                return BitConverter.ToInt64(FBuffer, FReadCursor - sizeof(Int64));
            }
            else
                return aDefaultValue;
        }
        public Single ReadSingle(Single aDefaultValue = 0)
        {
            if (sizeof(Single) <= ReadAvailable)
            {
                FReadCursor += sizeof(Single);
                return BitConverter.ToSingle(FBuffer, FReadCursor - sizeof(Single));
            }
            else
                return aDefaultValue;
        }
        public Double ReadDouble(Double aDefaultValue = 0)
        {
            if (sizeof(Double) <= ReadAvailable)
            {
                FReadCursor += sizeof(Double);
                return BitConverter.ToDouble(FBuffer, FReadCursor - sizeof(Double));
            }
            else
                return aDefaultValue;
        }
        public string ReadString(string aDefaultValue = "")
        {
            string s;
            if (Read(out s))
                return s;
            else
                return aDefaultValue;
        }
        public byte[] ReadBytes(UInt64 aLen)
        {
            byte[] bytes = new byte[aLen];
            if (Read(ref bytes))
                return bytes;
            else
                return new byte[0];
        }
        // read size and data and store as a whole WITHOUT size (size=length buffer)
        public TByteBuffer ReadByteBuffer()
        {
            Int32 Len;
            if (Read(out Len))
            {
                if (Len <= ReadAvailable)
                {
                    TByteBuffer Buffer = new TByteBuffer(Len);
                    Array.Copy(FBuffer, FReadCursor, Buffer.FBuffer, Buffer.FWriteCursor, Len);
                    Buffer.Written(Len);
                    FReadCursor += Len;
                    return Buffer;
                }
                else
                    return null;
            }
            else
                return null;
        }
        // QRead (no checking)
        public void QRead(out bool aValue)
        {
            aValue = BitConverter.ToBoolean(FBuffer, FReadCursor);
            FReadCursor += sizeof(bool);
        }
        public void QRead(out byte aValue)
        {
            aValue = FBuffer[FReadCursor];
            FReadCursor++;
        }
        public void QRead(out Int32 aValue)
        {
            aValue = BitConverter.ToInt32(FBuffer, FReadCursor);
            FReadCursor += sizeof(Int32);
        }
        public void QRead(out Int64 aValue)
        {
            aValue = BitConverter.ToInt64(FBuffer, FReadCursor);
            FReadCursor += sizeof(Int64);
        }
        public void QRead(out Single aValue)
        {
            aValue = BitConverter.ToSingle(FBuffer, FReadCursor);
            FReadCursor += sizeof(Single);
        }
        public void QRead(out Double aValue)
        {
            aValue = BitConverter.ToDouble(FBuffer, FReadCursor);
            FReadCursor += sizeof(Double);
        }
        public void QRead(out string aValue)
        {
            Int32 Len;
            QRead(out Len);
            aValue = Encoding.UTF8.GetString(FBuffer, FReadCursor, Len);
            FReadCursor += Len;
        }
        public void QRead(ref byte[] aValue)
        {
            Array.Copy(FBuffer, FReadCursor, aValue, 0, aValue.Length);
            FReadCursor += aValue.Length;
        }
        // read rest
        public void ReadRest(ref TByteBuffer aValue)
        {
            if (ReadAvailable > 0)
            {
                if (ReadAvailable > aValue.WriteAvailable)
                    aValue.Length = aValue.FWriteCursor + ReadAvailable;
                Array.Copy(FBuffer, FReadCursor, aValue.FBuffer, FWriteCursor, ReadAvailable);
            }
        }
        public byte[] ReadRest()
        {
            byte[] res = new byte[ReadAvailable];
            if (res.Length > 0)
                Array.Copy(FBuffer, FReadCursor, res, 0, res.Length);
            return res;
        }
        public void SkipReading(int aValueSize)
        {
            FReadCursor += aValueSize;
        }
        // peek
        public bool Peek(out bool aValue, int aOffset)
        {
            if (aOffset + sizeof(bool) <= ReadAvailable)
            {
                aValue = BitConverter.ToBoolean(FBuffer, FReadCursor + aOffset);
                return true;
            }
            else
            {
                aValue = false;
                return false;
            }
        }
        public bool Peek(out byte aValue, int aOffset)
        {
            if (aOffset + sizeof(byte) <= ReadAvailable)
            {
                aValue = FBuffer[FReadCursor + aOffset];
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Peek(out Int32 aValue, int aOffset)
        {
            if (aOffset + sizeof(Int32) <= ReadAvailable)
            {
                aValue = BitConverter.ToInt32(FBuffer, FReadCursor + aOffset);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Peek(out Int64 aValue, int aOffset)
        {
            if (aOffset + sizeof(Int64) <= ReadAvailable)
            {
                aValue = BitConverter.ToInt64(FBuffer, FReadCursor + aOffset);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Peek(out Single aValue, int aOffset)
        {
            if (aOffset + sizeof(Single) <= ReadAvailable)
            {
                aValue = BitConverter.ToSingle(FBuffer, FReadCursor + aOffset);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Peek(out Double aValue, int aOffset)
        {
            if (aOffset + sizeof(Double) <= ReadAvailable)
            {
                aValue = BitConverter.ToDouble(FBuffer, FReadCursor + aOffset);
                return true;
            }
            else
            {
                aValue = 0;
                return false;
            }
        }
        public bool Peek(out string aValue, int aOffset)
        {
            Int32 Len;
            if (Peek(out Len, aOffset))
            {
                if (aOffset + sizeof(Int32) + Len <= ReadAvailable)
                {
                    aValue = Encoding.UTF8.GetString(FBuffer, FReadCursor + aOffset + sizeof(Int32), Len);
                    return true;
                }
                else
                {
                    aValue = "";
                    return false;
                }
            }
            else
            {
                aValue = "";
                return false;
            }
        }
        // peek type result
        public bool PeekBoolean(int aOffset, bool aDefaultValue)
        {
            if (sizeof(bool) + aOffset <= ReadAvailable)
                return BitConverter.ToBoolean(FBuffer, FReadCursor + aOffset);
            else
                return aDefaultValue;
        }
        public byte PeekByte(int aOffset, byte aDefaultValue)
        {
            if (sizeof(byte) + aOffset <= ReadAvailable)
                return FBuffer[FReadCursor + aOffset];
            else
                return aDefaultValue;
        }
        public Int32 PeekInt32(int aOffset, Int32 aDefaultValue)
        {
            if (sizeof(Int32) + aOffset <= ReadAvailable)
                return BitConverter.ToInt32(FBuffer, FReadCursor + aOffset);
            else
                return aDefaultValue;
        }
        public Int64 PeekInt64(int aOffset, Int64 aDefaultValue)
        {
            if (sizeof(Int64) + aOffset <= ReadAvailable)
                return BitConverter.ToInt64(FBuffer, FReadCursor + aOffset);
            else
                return aDefaultValue;
        }
        public Single PeekSingle(int aOffset, Single aDefaultValue)
        {
            if (sizeof(Single) + aOffset <= ReadAvailable)
                return BitConverter.ToSingle(FBuffer, FReadCursor + aOffset);
            else
                return aDefaultValue;
        }
        public Double PeekDouble(int aOffset, Double aDefaultValue)
        {
            if (sizeof(Double) + aOffset <= ReadAvailable)
                return BitConverter.ToDouble(FBuffer, FReadCursor + aOffset);
            else
                return aDefaultValue;
        }
        public string PeekString(int aOffset, string aDefaultValue)
        {
            string s;
            if (Peek(out s, aOffset))
                return s;
            else
                return aDefaultValue;
        }
        public bool Compare(byte[] aValue, int aOffset = 0)
        {
            if (aOffset + aValue.Length <= ReadAvailable)
            {
                for (int i = 0; i < aValue.Length; i++)
                {
                    if (FBuffer[aOffset + FReadCursor + i] != aValue[i])
                        return false;
                }
                return true;
            }
            else
                return false;
        }
        // preparing
        public int PrepareStart()
        {
            FPrepareCursor = FWriteCursor;
            return FPrepareCursor;
        }
        public void Prepare(bool aValue) { FPrepareCursor += sizeof(bool); }
        public void Prepare(byte aValue) { FPrepareCursor += sizeof(byte); }
        public void Prepare(Int32 aValue) { FPrepareCursor += sizeof(Int32); }
        public void Prepare(Int64 aValue) { FPrepareCursor += sizeof(Int64); }
        public void Prepare(Single aValue) { FPrepareCursor += sizeof(Single); }
        public void Prepare(Double aValue) { FPrepareCursor += sizeof(Double); }
        public void Prepare(string aValue) { FPrepareCursor += sizeof(Int32) + Encoding.UTF8.GetBytes(aValue).Length; }
        public void Prepare(byte[] aValue) { FPrepareCursor += aValue.Length; }
        // prepare all readable data WITH size
        public void Prepare(TByteBuffer aValue) { FPrepareCursor += sizeof(Int32) + aValue.ReadAvailable; }
        public int PrepareSize(int aValueSize)
        {
            int res = FPrepareCursor;
            FPrepareCursor += aValueSize;
            return res;
        }
        public void PrepareApply()
        {
            if (Length < FPrepareCursor)
                Length = FPrepareCursor;
            FPBWriteCursor = FPrepareCursor;
        }
        public void PrepareApplyAndTrim()
        {
            if (Length != FPrepareCursor)
                Length = FPrepareCursor;
            FPBWriteCursor = FPrepareCursor;
        }
        // writing
        public void WriteStart(int aIndex = 0) { FWriteCursor = aIndex; }
        public int WriteAvailable { get { return Length - FWriteCursor; } }
        // Write
        public void Write(bool aValue)
        {
            if (sizeof(bool) > WriteAvailable)
                Length = FWriteCursor + sizeof(bool);
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(bool);
        }
        public void Write(byte aValue)
        {
            if (sizeof(byte) > WriteAvailable)
                Length = FWriteCursor + sizeof(byte);
            FBuffer[FWriteCursor] = aValue;
            FWriteCursor += sizeof(byte);
        }
        public void Write(Int32 aValue)
        {
            if (sizeof(Int32) > WriteAvailable)
                Length = FWriteCursor + sizeof(Int32);
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Int32);
        }
        public void Write(Int64 aValue)
        {
            if (sizeof(Int64) > WriteAvailable)
                Length = FWriteCursor + sizeof(Int64);
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Int64);
        }
        public void Write(Single aValue)
        {
            if (sizeof(Single) > WriteAvailable)
                Length = FWriteCursor + sizeof(Single);
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Single);
        }
        public void Write(Double aValue)
        {
            if (sizeof(Double) > WriteAvailable)
                Length = FWriteCursor + sizeof(Double);
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Double);
        }
        public void Write(string aValue)
        {
            byte[] s = Encoding.UTF8.GetBytes(aValue);
            Int32 Len = s.Length;
            if (sizeof(Int32) + Len > WriteAvailable)
                Length = FWriteCursor + sizeof(Int32) + Len;
            BitConverter.GetBytes(Len).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Int32);
            if (Len > 0)
            {
                s.CopyTo(FBuffer, FWriteCursor);
                FWriteCursor += Len;
            }
        }
        // write array if byte WITHOUT size
        public void Write(byte[] aValue)
        {
            if (aValue.Length > WriteAvailable)
                Length = FWriteCursor + aValue.Length;
            aValue.CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += aValue.Length;
        }
        // write all readable data WITH size
        public void Write(TByteBuffer aValue)
        {
            if (sizeof(Int32) + aValue.ReadAvailable > WriteAvailable)
                Length = FWriteCursor + sizeof(Int32) + aValue.ReadAvailable;
            QWrite(aValue.ReadAvailable);
            Array.Copy(aValue.FBuffer, aValue.ReadCursor, FBuffer, FWriteCursor, aValue.ReadAvailable);
            FWriteCursor += aValue.ReadAvailable;
        }
        // QWrite (no room checking)
        public void QWrite(bool aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(bool);
        }
        public void QWrite(byte aValue)
        {
            FBuffer[FWriteCursor] = aValue;
            FWriteCursor += sizeof(byte);
        }
        public void QWrite(Int32 aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Int32);
        }
        public void QWrite(Int64 aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Int64);
        }
        public void QWrite(Single aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Single);
        }
        public void QWrite(Double aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Double);
        }
        public void QWrite(string aValue)
        {
            byte[] s = Encoding.UTF8.GetBytes(aValue);
            Int32 Len = s.Length;
            BitConverter.GetBytes(Len).CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += sizeof(Int32);
            if (Len > 0)
            {
                s.CopyTo(FBuffer, FWriteCursor);
                FWriteCursor += Len;
            }
        }
        // write array if byte WITHOUT size
        public void QWrite(byte[] aValue)
        {
            aValue.CopyTo(FBuffer, FWriteCursor);
            FWriteCursor += aValue.Length;
        }
        // write, with no checking, all readable data WITH size
        public void QWrite(TByteBuffer aValue)
        {
            QWrite(aValue.ReadAvailable);
            Array.Copy(aValue.FBuffer, aValue.ReadCursor, FBuffer, FWriteCursor, aValue.ReadAvailable);
            FWriteCursor += aValue.ReadAvailable;
        }
        public bool Written(int aValueSize)
        {
            FWriteCursor += aValueSize;
            return WriteAvailable >= 0;
        }
        // writing apply
        public void WriteApply() { if (FWriteCursor != Length) Length = FWriteCursor; }
        // protocol buffers
        public enum TWireType
        {
            wtVarInt = 0,
            wt64Bit = 1,
            wtLengthDelimited = 2,
            wtStartGroup = 3,
            wtEndGroup = 4,
            wt32Bit = 5
        }
        public UInt64 PBPrepare(bool aValue)
        {
            FPrepareCursor += sizeof(bool);
            return sizeof(bool);
        }
        public UInt64 PBPrepare(UInt64 aValue)
        {
            UInt64 res;
            if (aValue<128)
                res = 1;
            else if (aValue<16384)
                res = 2;
            else if (aValue < 2097152)
                res = 3;
            else
            {
                // 4 bytes or more: change to dynamic size detection
                res = 4;
                aValue = aValue >> (7 * 4);
                while (aValue > 0)
                {
                    res++;
                    aValue = aValue >> 7;
                }
            }
            FPrepareCursor = FPrepareCursor+(int)res;
            return res;
        }
        public UInt64 PBPrepare(Int64 aValue)
        {
            if (aValue < 0)
                return PBPrepare((UInt64)(((-aValue) << 1)) | 1);
            else
                return PBPrepare((UInt64)(aValue << 1));
        }
        public UInt64 PBPrepare(UInt32 aValue)
        {
            return PBPrepare((UInt64)aValue);
        }
        public UInt64 PBPrepare(Int32 aValue)
        {
            return PBPrepare((Int64)aValue);
        }
        public UInt64 PBPrepare(string aValue)
        {
            int len = Encoding.UTF8.GetBytes(aValue).Length;
            int res = (int)PBPrepare(len) + len;
            FPrepareCursor += len;
            return (UInt64)res;
        }
        public UInt64 PBPrepare(byte[] aValue)
        {
            int res = aValue.Length;
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepare(byte[] aValue, int aLen)
        {
            int res = aLen;
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepare(Single aValue)
        {
            int res = sizeof(Single);
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepare(Double aValue)
        {
            int res = sizeof(Double);
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepareFixed(UInt64 aValue)
        {
            int res = sizeof(UInt64);
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepareFixed(Int64 aValue)
        {
            int res = sizeof(Int64);
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepareFixed(UInt32 aValue)
        {
            int res = sizeof(UInt32);
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepareFixed(Int32 aValue)
        {
            int res = sizeof(Int32);
            FPrepareCursor += res;
            return (UInt64)res;
        }
        public UInt64 PBPrepareFieldInfo(UInt64 aTag, TWireType aWireType)
        {
            return PBPrepare((UInt64)((aTag << 3) | (UInt32)aWireType));
        }
        public UInt64 PBPrepareField(UInt64 aTag, bool aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, UInt64 aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, Int64 aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, UInt32 aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, Int32 aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, string aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wtLengthDelimited);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, byte[] aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepare(res);
            res += PBPrepareFieldInfo(aTag, TWireType.wtLengthDelimited);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, Single aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wt32Bit);
            return res;
        }
        public UInt64 PBPrepareField(UInt64 aTag, Double aValue)
        {
            UInt64 res = PBPrepare(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wt64Bit);
            return res;
        }
        public UInt64 PBPrepareFixedField(UInt64 aTag, UInt64 aValue)
        {
            UInt64 res = PBPrepareFixed(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wt64Bit);
            return res;
        }
        public UInt64 PBPrepareFixedField(UInt64 aTag, Int64 aValue)
        {
            UInt64 res = PBPrepareFixed(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wt64Bit);
            return res;
        }
        public UInt64 PBPrepareFixedField(UInt64 aTag, UInt32 aValue)
        {
            UInt64 res = PBPrepareFixed(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wt32Bit);
            return res;
        }
        public UInt64 PBPrepareFixedField(UInt64 aTag, Int32 aValue)
        {
            UInt64 res = PBPrepareFixed(aValue);
            res += PBPrepareFieldInfo(aTag, TWireType.wt32Bit);
            return res;
        }
        // PBQWrite functions work backwards (reverse order) on own cursor (FPBWriteCursor) because size is only known after writing (VarInt)
        public UInt64 PBQWrite(bool aValue)
        {
            return aValue ? PBQWrite((UInt64)1) : PBQWrite((UInt64)0);
        }
        public UInt64 PBQWrite(UInt64 aValue)
        {
            if (aValue < 128)
            {
                FBuffer[--FPBWriteCursor] = (byte)aValue;
                return 1;
            }
            else
            {
                UInt64 res = PBQWrite((UInt64)aValue >> 7) + 1;
                FBuffer[--FPBWriteCursor] = (byte)((aValue & 0x7F) | 0x80);
                return res;
            }
        }
        public UInt64 PBQWrite(Int64 aValue)
        {
            if (aValue<0)
                return PBQWrite((UInt64)(((-aValue) << 1) | 1));
            else
                return PBQWrite((UInt64)(aValue << 1));
        }
        public UInt64 PBQWrite(UInt32 aValue)
        {
            return PBQWrite((UInt64)aValue);
        }
        public UInt64 PBQWrite(Int32 aValue)
        {
            return PBQWrite((Int64)aValue);
        }
        public UInt64 PBQWrite(string aValue)
        {
            byte[] s = Encoding.UTF8.GetBytes(aValue);
            UInt64 res = PBQWrite(s);
            res += PBQWrite((UInt64)res);
            return res;
        }
        public UInt64 PBQWrite(byte[] aValue)
        {
            int res = aValue.Length;
            aValue.CopyTo(FBuffer, FPBWriteCursor-res);
            FPBWriteCursor -= res;
            return (UInt64)res;
        }
        public UInt64 PBQWrite(byte[] aValue, int aLen)
        {
            int res = aLen;
            aValue.CopyTo(FBuffer, FPBWriteCursor - res);
            FPBWriteCursor -= res;
            return (UInt64)res;
        }
        public UInt64 PBQWrite(Single aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FPBWriteCursor-sizeof(Single));
            FPBWriteCursor -= sizeof(Single);
            return sizeof(Single);
        }
        public UInt64 PBQWrite(Double aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FPBWriteCursor-sizeof(Double));
            FPBWriteCursor -= sizeof(Double);
            return sizeof(Double);
        }
        public UInt64 PBQWriteFixed(UInt64 aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FPBWriteCursor-sizeof(UInt64));
            FPBWriteCursor -= sizeof(UInt64);
            return sizeof(UInt64);
        }
        public UInt64 PBQWriteFixed(Int64 aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FPBWriteCursor-sizeof(Int64));
            FPBWriteCursor -= sizeof(Int64);
            return sizeof(Int64);
        }
        public UInt64 PBQWriteFixed(UInt32 aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FPBWriteCursor-sizeof(UInt32));
            FPBWriteCursor -= sizeof(UInt32);
            return sizeof(UInt32);
        }
        public UInt64 PBQWriteFixed(Int32 aValue)
        {
            BitConverter.GetBytes(aValue).CopyTo(FBuffer, FPBWriteCursor-sizeof(Int32));
            FPBWriteCursor -= sizeof(Int32);
            return sizeof(Int32);
        }
        public UInt64 PBQWriteFieldInfo(UInt64 aTag, TWireType aWiretype)
        {
            return PBQWrite((UInt64)((aTag << 3) | (UInt32)aWiretype));
        }
        public UInt64 PBQWriteField(UInt64 aTag, bool aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, UInt64 aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, Int64 aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, UInt32 aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, Int32 aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wtVarInt);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, string aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wtLengthDelimited);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, Single aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wt32Bit);
            return res;
        }
        public UInt64 PBQWriteField(UInt64 aTag, Double aValue)
        {
            UInt64 res = PBQWrite(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wt64Bit);
            return res;
        }
        public UInt64 PBQWriteFixedField(UInt64 aTag, UInt64 aValue)
        {
            UInt64 res = PBQWriteFixed(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wt64Bit);
            return res;
        }
        public UInt64 PBQWriteFixedField(UInt64 aTag, Int64 aValue)
        {
            UInt64 res = PBQWriteFixed(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wt64Bit);
            return res;
        }
        public UInt64 PBQWriteFixedField(UInt64 aTag, UInt32 aValue)
        {
            UInt64 res = PBQWriteFixed(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wt32Bit);
            return res;
        }
        public UInt64 PBQWriteFixedField(UInt64 aTag, Int32 aValue)
        {
            UInt64 res = PBQWriteFixed(aValue);
            res += PBQWriteFieldInfo(aTag, TWireType.wt32Bit);
            return res;
        }
        // PB peek
        public UInt64 PBPeekUInt64(int aOffset=0)
        {
            byte b = FBuffer[FReadCursor+aOffset];
            if ( b< 128)
                return b;
            else
                return (PBPeekUInt64(aOffset+1) << 7) | (byte)(b & 0x7F);
        }
        public Int64 PBPeekInt64()
        {
            UInt64 res = PBPeekUInt64();
            return ((res & 1) == 1) ? -(Int64)(res >> 1) : (Int64)(res >> 1);
        }
        // PB read
        public UInt64 PBRead(out bool aValue)
        {
            UInt64 ui64;
            UInt64 res = PBRead(out ui64);
            aValue = ui64 != 0;
            return res;
        }
        public UInt64 PBRead(out UInt64 aValue)
        {
            byte b = FBuffer[FReadCursor++]; 
            if (b<128)
            {
                aValue = b;
                return 1;
            }
            else
            {
                UInt64 res = PBRead(out aValue) + 1;
                aValue = (aValue << 7) | ((UInt64)b & 0x7F);
                return res;
            }
        }
        public UInt64 PBRead(out Int64 aValue)
        {
            UInt64 ui64;
            UInt64 res = PBRead(out ui64);
            aValue = ((ui64 & 1) == 1) ? -(Int64)(ui64 >> 1) : (Int64)(ui64 >> 1);
            return res;
        }
        public UInt64 PBRead(out UInt32 aValue)
        {
            UInt64 ui64;
            UInt64 res = PBRead(out ui64);
            aValue = (UInt32)ui64;
            return res;
        }
        public UInt64 PBRead(out Int32 aValue)
        {
            Int64 i64;
            UInt64 res = PBRead(out i64);
            aValue = (Int32)i64;
            return res;
        }
        public UInt64 PBRead(out string aValue)
        {
            UInt64 len;
            UInt64 res = PBRead(out len);
            int iLen = (int)len;
            res = res+(UInt64)iLen;
            if (0<iLen && iLen <= ReadAvailable)
            {
                aValue = Encoding.UTF8.GetString(FBuffer, FReadCursor, iLen);
                FReadCursor += iLen;
            }
            else
            {
                aValue = "";
            }
            return res;
        }
        public UInt64 PBReadFixed(out UInt64 aValue)
        {
            if (sizeof(UInt64) <= ReadAvailable)
            {
                aValue = BitConverter.ToUInt64(FBuffer, FReadCursor);
                FReadCursor += sizeof(UInt64);
                return sizeof(UInt64);
            }
            else
            {
                aValue = 0;
                return 0;
            }
        }
        public UInt64 PBReadFixed(out Int64 aValue)
        {
            Read(out aValue);
            return sizeof(Int64);
        }
        public UInt64 PBReadFixed(out UInt32 aValue)
        {
            if (sizeof(UInt32) <= ReadAvailable)
            {
                aValue = BitConverter.ToUInt32(FBuffer, FReadCursor);
                FReadCursor += sizeof(UInt32);
                return sizeof(UInt32);
            }
            else
            {
                aValue = 0;
                return 0;
            }
        }
        public UInt64 PBReadFixed(out Int32 aValue)
        {
            Read(out aValue);
            return sizeof(Int32);
        }
        public string PBReadAnsiString()
        {
            string res;
            PBRead(out res);
            return res;
        }
        public byte[] PBReadBytes()
        {
            UInt64 len;
            PBRead(out len);
            int iLen = (int)len;
            if (0 < iLen && iLen <= ReadAvailable)
            {
                byte[] res = new byte[iLen];
                Array.Copy(FBuffer, FReadCursor, res, 0, iLen);
                FReadCursor += iLen;
                return res;
            }
            else
                return new byte[0];
        }
        public UInt64 PBReadFieldInfo(out UInt64 aTag, out TWireType aWireType)
        {
            UInt64 ui64;
            UInt64 res = PBRead(out ui64);
            aWireType = (TWireType)(ui64 & 7);
            aTag = ui64 >> 3;
            return res;
        }
        // PB skip a specific wire type
        public UInt64 PBSkip(TWireType aWireType)
        {
            UInt64 varInt;
            UInt64 res = 0;
            switch (aWireType)
            {
                case TWireType.wtVarInt:
                    res = PBRead(out varInt);
                    break;
                case TWireType.wt64Bit:
                    res = 8;
                    SkipReading((int)res);
                    break;
                case TWireType.wtLengthDelimited:
                    res = PBRead(out varInt);
                    res = res + varInt;
                    SkipReading((int)varInt);
                    break;
                case TWireType.wtStartGroup:
                    throw new Exception("'Start group' wire type is not supported");
                case TWireType.wtEndGroup:
                    throw new Exception("'End group' wire type is not supported");
                case TWireType.wt32Bit:
                    res = 4;
                    SkipReading((int)res);
                    break;
                default:
                    res = 0;
                    break;
            }
            return res;
        }
        // there are no PBReadField functions because we only now the type after reading the wire type and tag
    }
}
