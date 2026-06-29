using Unity.Collections;
using Unity.Entities;

namespace Game.Script.Tool
{
    public struct BlobStringTool
    {
        public static bool BsEqualFs(ref BlobString blob, in FixedString64Bytes targetFs)
        {
            FixedString64Bytes temp = default;
            var error = blob.CopyTo(ref temp);
            if (error == ConversionError.Overflow)
                return false;
            return temp == targetFs;
        }
    }
}