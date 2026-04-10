
using System;

public static class CustomRandom
{

    public static int Int(int min, int max)
    {
        return UnityEngine.Random.Range(min, max);
    }
    public static float Float(float min, float max)
    {
        return UnityEngine.Random.Range(min, max);
    }

}