using Unity.Mathematics;

public struct AdjacentIntOffsetsClockwise
{
    public int2 this[int index]
    {
        get
        {
            switch(index)
            {
                case 0:
                    return new int2(0, 1);
                case 1:
                    return new int2(1, 1);
                case 2:
                    return new int2(1, 0);
                case 3:
                    return new int2(1, -1);
                case 4:
                    return new int2(0, -1);
                case 5:
                    return new int2(-1, -1);
                case 6:
                    return new int2(-1, 0);
                case 7:
                    return new int2(-1, 1);
                default:
                    throw new System.IndexOutOfRangeException("Adjacent offset index out of range");
            }
        }
    }
}
