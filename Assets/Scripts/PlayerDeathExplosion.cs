using UnityEngine;

public class PlayerDeathExplosion : MonoBehaviour
{
    public PlayerDeathStreamer[] _deathStreamers;

    public void SetColour(Color colour)
    {
        for(int i = 0; i < _deathStreamers.Length; i++)
        {
            _deathStreamers[i].SetColour(colour);
        }
    }

}
