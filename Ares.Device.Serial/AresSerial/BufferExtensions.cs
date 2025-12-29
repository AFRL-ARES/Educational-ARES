using System;
using System.Collections.Generic;

namespace Ares.Device.Serial
{
  internal static class BufferExtensions
  {
    public static void RemoveBytes(this IList<SerialBlock> buffer, ArraySegment<byte> bytes)
    {
      if (buffer is not List<SerialBlock> bufferList)
        return;

      if (bytes.Count == 0)
        return;

      var (startBlock, endBlock) = bufferList.GetSerialBlockRange(bytes.Offset, bytes.Count);
      if (startBlock is null || endBlock is null)
        return;

      var startIdx = startBlock.BlockIdx;
      var endIdx = endBlock.BlockIdx;

      if (startBlock.DataIdx > 0)
      {
        var (first, second) = SplitBlock(bufferList[startIdx], startBlock.DataIdx);
        bufferList[startIdx] = first;
        bufferList.Insert(startIdx + 1, second);

        if (startIdx == endIdx)
          endBlock = new BlockResult(endIdx + 1, endBlock.DataIdx - startBlock.DataIdx);
        else
          endBlock = new BlockResult(endIdx + 1, endBlock.DataIdx);

        startIdx++;
        endIdx++;
      }

      if (endBlock.DataIdx < bufferList[endIdx].Data.Length - 1)
      {
        var (first, second) = SplitBlock(bufferList[endIdx], endBlock.DataIdx + 1);
        bufferList[endIdx] = first;
        bufferList.Insert(endIdx + 1, second);
      }

      bufferList.RemoveRange(startIdx, endIdx - startIdx + 1);
    }

    /// <summary>
    /// Splits a block into two blocks based on the given index pivot.
    /// First block returned is up to but not including the given index.
    /// </summary>
    /// <param name="block"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    private static (SerialBlock, SerialBlock) SplitBlock(SerialBlock block, int idx)
    {
      var block1 = new SerialBlock(block.Data[..idx], block.Timestamp);
      var block2 = new SerialBlock(block.Data[idx..], block.Timestamp);
      return (block1, block2);
    }

    public static (BlockResult?, BlockResult?) GetSerialBlockRange(this IList<SerialBlock> buffer, int offset, int length)
    {
      var currentLength = 0;
      BlockResult? start = null;
      BlockResult? end = null;
      for (int i = 0; i < buffer.Count; i++)
      {
        currentLength += buffer[i].Data.Length;

        if (currentLength > offset)
        {
          start = new BlockResult(i, buffer[i].Data.Length - (currentLength - offset));
          break;
        }
      }
      if (start is null)
        return (null, null);

      currentLength -= offset;
      if (currentLength >= length)
        return (start, new BlockResult(start.BlockIdx, start.DataIdx + length - 1));

      var lengthRemaining = length - currentLength;
      for (int i = start.BlockIdx + 1; i < buffer.Count; i++)
      {
        currentLength += buffer[i].Data.Length;
        if (currentLength >= length)
        {
          end = new BlockResult(i, lengthRemaining - 1);
          break;
        }

        lengthRemaining -= buffer[i].Data.Length;
      }

      return (start, end);
    }
  }

  internal record BlockResult(int BlockIdx, int DataIdx);
}
