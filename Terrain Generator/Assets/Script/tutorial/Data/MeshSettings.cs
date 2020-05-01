using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public const int numOfSupportedLOD = 5;
    public const int supportedBlockSize = 9;
    public static readonly int[] supportedSizes = { 48, 72, 96, 120, 144, 168, 196, 216, 240 };

    [Range(0, supportedBlockSize - 1)]
    public int blockSizeIndex;

    public float meshScale = 5f;
    //number of vertices per line of mesh rendered at LOD = 0
    //this number includes the 2 extra vertices for the border
    public int verticesNumPerLine
    {
        get
        {
            return supportedSizes[blockSizeIndex] + 5;
        }
    }

    public float meshWorldSize
    {
        get
        {
            return (verticesNumPerLine - 3) * meshScale;
        }
    }

}
