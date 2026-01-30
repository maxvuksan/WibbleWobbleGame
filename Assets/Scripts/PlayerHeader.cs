using UnityEngine;

public class PlayerHeader : MonoBehaviour
{
    public ulong Index
    {
        get { return _index;}
    }

    private ulong _index;

    public void SetIndex(ulong index)
    {
        this._index = index;
    }

}
