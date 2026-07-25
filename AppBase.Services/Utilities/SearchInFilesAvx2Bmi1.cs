using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace AppBase.Services.Utilities;

public readonly ref struct SearchInFilesAvx2Bmi1
{
    private static readonly byte[] BOM = new byte[] { (byte)239, (byte)187, (byte)191 };
    private static readonly Vector256<byte> vector32 = Vector256.Create((byte)32);

    private readonly ReadOnlySpan<byte> bytesToFind;
    private readonly int toSearchLen;
    private const int BUFFER_SIZE = 65_536;
    private readonly Vector256<byte> Avec;
    private readonly Vector256<byte> Zvec;

    public SearchInFilesAvx2Bmi1(ReadOnlySpan<char> path, ReadOnlySpan<char> toSearch)
    {
        bytesToFind = Encoding.UTF8.GetBytes(toSearch.ToArray());
        toSearchLen = bytesToFind.Length;
        Avec = Vector256.Create((byte)((byte)bytesToFind[0] | (byte)32));
        Zvec = Vector256.Create((byte)((byte)bytesToFind[toSearchLen - 1] | (byte)32));

        //AvecX = new Vector<byte>((byte)((byte)bytesToFind[0] | (byte)32));
        //ZvecX = new Vector<byte>((byte)((byte)bytesToFind[toSearchLen - 1] | (byte)32));
    }

    public int FindInFileSmallSteps(ReadOnlySpan<char> path, ReadOnlySpan<char> toSearch)
    {
        if (!Avx2.IsSupported || !Bmi1.IsSupported)
        {
            return SearchInFilesAvx2Bmi1.SearchInFileOriginal(path.ToString(), toSearch.ToString()) ? 1 : -1;
        }

        using var fs = new FileStream(path.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: BUFFER_SIZE, FileOptions.SequentialScan);
        int bomAdd = 0;

        byte[] borrowed = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
        Span<byte> buffer = borrowed.AsSpan();
        int readed = BUFFER_SIZE;
        bool firstTime = true;
        while (readed > 0)
        {
            int r = 0;
            if (firstTime)
            {
                readed = fs.Read(buffer);
                firstTime = false;
                if (buffer.StartsWith(BOM))
                {
                    bomAdd = 3;
                }
                r = DoSearchSimdAvxUTF8BytesFixedIgnoreCase(buffer.Slice(0, readed), bytesToFind);
                //r = DoSearchSimd(buffer.Slice(0, readed), bytesToFind);
                if (r != -1)
                {
                    ArrayPool<byte>.Shared.Return(borrowed);
                    return (int)fs.Position - readed + r + bomAdd;
                }
            }
            else
            {
                buffer.Slice(BUFFER_SIZE - toSearchLen).CopyTo(buffer.Slice(0, toSearchLen)); // Handle boundary crossings by stepping back slightly.
                readed = fs.Read(buffer.Slice(toSearchLen));
                r = DoSearchSimdAvxUTF8BytesFixedIgnoreCase(buffer.Slice(0, readed + toSearchLen), bytesToFind);
                //r = DoSearchSimd(buffer.Slice(0, readed + toSearchLen), bytesToFind);

                if (r != -1)
                {
                    ArrayPool<byte>.Shared.Return(borrowed);
                    return (int)fs.Position - readed + r - toSearchLen + bomAdd;
                }
            }
        }
        ArrayPool<byte>.Shared.Return(borrowed);
        return -1;
    }

    private unsafe int DoSearchSimdAvxUTF8BytesFixedIgnoreCase(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> bytesToFind)
    {
        int toSearchLen = bytesToFind.Length;
        int FileContentLength = bytes.Length;

        fixed (byte* ptr = bytes)
        {
            int vectorLength = 256 / 2 / 4;
            if (vectorLength > bytes.Length)
            {
                return DoSearchManualIgnoreCase(bytes, bytesToFind);
            }

            int i = 0;
            for (; i <= FileContentLength - vectorLength - toSearchLen; i += vectorLength)
            {
                var viA = Avx2.LoadVector256(ptr + i);
                var viZ = Avx2.LoadVector256(ptr + i + toSearchLen - 1);
                viA = Avx2.Or(viA, vector32); // to lower 'A' | 32 = 'a'
                viZ = Avx2.Or(viZ, vector32); // to lower 'A' | 32 = 'a'

                var veA = Avx2.CompareEqual(viA, Avec);
                var veZ = Avx2.CompareEqual(viZ, Zvec);
                var andVec = Avx2.And(veA, veZ);

                uint tempMask = (uint)Avx2.MoveMask(andVec);

                if (tempMask > 0)
                {
                    if (toSearchLen == 2)
                    {
                        return i;
                    }

                    int cnt = BitOperations.PopCount(tempMask);
                    int l = i;
                    for (int j = 0; j < cnt; j++)
                    {
                        int offset = BitOperations.TrailingZeroCount(tempMask);
                        l += offset;

                        if (EqualsBytesIgonreCase(bytes.Slice(l), bytesToFind))
                        {
                            return l;
                        }

                        tempMask >>= (offset + 1);
                        l++;
                    }
                }
            }
            int w = DoSearchManualIgnoreCase(bytes.Slice(i), bytesToFind);

            if (w > 0)
            {
                return i + w;
            }
            return -1;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool EqualsBytesIgonreCase(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> bytesToFind)
    {
        for (int i = 0; i < bytesToFind.Length; i++)
        {
            byte c1 = bytes[i];
            byte c2 = bytesToFind[i];
            byte c3 = (byte)(c1 ^ c2);
            if (c3 != 0 && (c3 != 32 || (c1 | 32) < 97 || (c1 | 32) > 122))
            {
                return false;
            }
        }
        return true;
    }

    private static int DoSearchManualIgnoreCase(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> bytesToFind)
    {
        int toSearchLen = bytesToFind.Length;
        int FileContentLength = bytes.Length;
        if (FileContentLength < toSearchLen)
        {
            return -1;
        }

        byte startByte = (byte)(bytesToFind[0] | 32);
        byte endByte = (byte)(bytesToFind[toSearchLen - 1] | 32);
        int nn = FileContentLength - toSearchLen + 1;

        int i = 0;

        for (; i < nn - nn % 4; i += 4)
        {
            if ((bytes[i] | 32) == startByte && (bytes[i + toSearchLen - 1] | 32) == endByte && EqualsBytesIgonreCase(bytes.Slice(i), bytesToFind))
            {
                return i;
            }

            int i1 = i + 1;
            if ((bytes[i1] | 32) == startByte && (bytes[i1 + toSearchLen - 1] | 32) == endByte && EqualsBytesIgonreCase(bytes.Slice(i1), bytesToFind))
            {
                return i1;
            }

            int i2 = i + 2;
            if ((bytes[i2] | 32) == startByte && (bytes[i2 + toSearchLen - 1] | 32) == endByte && EqualsBytesIgonreCase(bytes.Slice(i2), bytesToFind))
            {
                return i2;
            }

            int i3 = i + 3;
            if ((bytes[i3] | 32) == startByte && (bytes[i3 + toSearchLen - 1] | 32) == endByte && EqualsBytesIgonreCase(bytes.Slice(i3), bytesToFind))
            {
                return i3;
            }
        }

        for (; i < nn; i++)
        {
            if ((bytes[i] | 32) == startByte && (bytes[i + toSearchLen - 1] | 32) == endByte && EqualsBytesIgonreCase(bytes.Slice(i), bytesToFind))
            {
                return i;
            }
        }
        return -1;
    }

    public static bool SearchInFileOriginal(string path, string searchText)
    {
        using (StreamReader s = new StreamReader(path, Encoding.UTF8, true, bufferSize: BUFFER_SIZE))
        {
            while (s.Peek() >= 0)
            {
                if (s.ReadLine().Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
