using UnityEngine;


public static class ObjectExtensions
{
    public static float ToFloat(this string data)
    {
        return string.IsNullOrWhiteSpace(data) ? 0f : float.Parse(data);
    }

    public static Vector3 ReversePosition(this Vector3 vector3)
    {
        return new Vector3(vector3.x, vector3.y, vector3.z * (-1f));
    }

    public static Quaternion ReverseRotation(this Vector3 vector3)
    {
        var quaternion = Quaternion.Euler(vector3);
        return new Quaternion(quaternion.x * (-1f), quaternion.y * (-1f), quaternion.z, quaternion.w);
    }
    //how to use:
    //var reversePos = new Vector3(-1.3f,3.4f,200f).ReversePosition();
    //var reverseQua = new Vector3(-0.062f,78,3.45f).ReverseRotation();
}
