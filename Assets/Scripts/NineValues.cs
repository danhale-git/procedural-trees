using Unity.Collections;

public struct NineValues<T> where T : struct
{
    public int Length;
    public T _0, _1, _2, _3, _4, _5, _6, _7, _8;

    public NineValues(NativeList<T> inputList)
    {
        if(inputList.Length > 9)
            throw new System.Exception("Input list for NineValues cannot be longer than 9 values");

        this.Length = inputList.Length;
        this._0 = Length <= 0 ? default(T) : inputList[0];
        this._1 = Length <= 1 ? default(T) : inputList[1];
        this._2 = Length <= 2 ? default(T) : inputList[2];
        this._3 = Length <= 3 ? default(T) : inputList[3];
        this._4 = Length <= 4 ? default(T) : inputList[4];
        this._5 = Length <= 5 ? default(T) : inputList[5];
        this._6 = Length <= 6 ? default(T) : inputList[6];
        this._7 = Length <= 7 ? default(T) : inputList[7];
        this._8 = Length <= 8 ? default(T) : inputList[8];
    }

    public T this[int index]
    {
        get
        {
            switch(index)
            {
                case 0: return _0;
                case 1: return _1;
                case 2: return _2;
                case 3: return _3;
                case 4: return _4;
                case 5: return _5;
                case 6: return _6;
                case 7: return _7;
                case 8: return _8;

                default: throw new System.IndexOutOfRangeException("Index "+index+" out of range 8");
            }
        }

        set
        {
            switch(index)
            {
                case 0: _0 = value; break;
                case 1: _1 = value; break;
                case 2: _2 = value; break;
                case 3: _3 = value; break;
                case 4: _4 = value; break;
                case 5: _5 = value; break;
                case 6: _6 = value; break;
                case 7: _7 = value; break;
                case 8: _8 = value; break;

                default: throw new System.IndexOutOfRangeException("Index "+index+" out of range 8");
            }
        }
    }
}