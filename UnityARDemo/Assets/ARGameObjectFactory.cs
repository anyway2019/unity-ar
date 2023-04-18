using System;
using UnityEngine;
using UnityEngine.Rendering;

public static class ARGameObjectFactory
{
    public static Vector3[] CameraPositions { get; set; }
    public static Vector3[] CameraRotations { get; set; }
    public static Vector3[] FilteredCameraPositions { get; set; }
    public static Quaternion[] TrajectoryOrientation { get; set; }

    private static bool IsValidVertex(Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) &&
                                                    !float.IsInfinity(v.y) && !float.IsNaN(v.z) &&
                                                    !float.IsInfinity(v.z);

    public static float CameraHeight { get; set; } = 1f;
    public static int[] LeftSideOffsetFrames { get; set; }
    public static float[] LeftSideOffsets { get; set; }
    public static int[] RightSideOffsetFrames { get; set; }
    public static float[] RightSideOffsets { get; set; }

    public static float RiderScale { get; set; } = 1f;
    public static bool LeftHanded { get; set; }
    private static float DefaultLeftSideOffset => !LeftHanded ? 4f : 0.0f;
    private static float DefaultRightSideOffset => !LeftHanded ? 0.0f : 4f;
    public static float RouteMargin { get; private set; } = 2f;

    public static bool HasLeftSideOffsets => LeftSideOffsetFrames != null && LeftSideOffsets != null;

    public static bool HasRightSideOffsets => RightSideOffsetFrames != null && RightSideOffsets != null;

    public static int FrameCount => CameraPositions.Length;
    public static double[] FrameDistances { get; set; }
    public static float[] TrajectoryCurvatures { get; set; }
    public static GameObject Trajectory { get; set; }

    public static void Init(Vector3[] positions, Vector3[] rotations,float leftOffset = 2f,float rightOffset = 2f,Material material = null)
    {
        CameraPositions = positions;
        CameraRotations = rotations;
        CreateFilteredCameraPositions();
        CreateFrameDistances();
        CreateTrajectoryOrientations(); 
        CreateTrajectoryCurvatures();
        if (Trajectory != null)
            GameObject.Destroy(Trajectory);
        Trajectory = CreateTrajectory(0, leftOffset, rightOffset);
        Trajectory.GetComponent<MeshRenderer>().material = material;
    }

    private static void CreateFilteredCameraPositions()
    {
        var length = CameraPositions.Length;
        var vector3Array = new Vector3[length];
        for (var index = 0; index < length; ++index)
            vector3Array[index] = CameraPositions[index];
        for (var index1 = 0; index1 < 3; ++index1)
        {
            for (var index2 = 1; index2 < vector3Array.Length - 1; ++index2)
                vector3Array[index2] =
                    (vector3Array[index2 - 1] + vector3Array[index2] + vector3Array[index2 + 1]) / 3f;
        }

        FilteredCameraPositions = vector3Array;
    }

    private static void CreateFrameDistances()
    {
        var length = CameraPositions.Length;
        FrameDistances = new double[length];
        FrameDistances[0] = 0.0;
        var num = 0.0;
        for (var index = 1; index < length; ++index)
        {
            num += (double)Vector3.Distance(CameraPositions[index - 1], CameraPositions[index]);
            FrameDistances[index] = num;
        }
    }

    private static void CreateTrajectoryOrientations()
    {
        var quaternionArray = new Quaternion[FilteredCameraPositions.Length];
        for (var index = 1; index < FilteredCameraPositions.Length - 1; ++index)
            quaternionArray[index] =
                Quaternion.LookRotation(FilteredCameraPositions[index + 1] - FilteredCameraPositions[index - 1],
                    Vector3.up);
        quaternionArray[0] = quaternionArray[1];
        quaternionArray[FilteredCameraPositions.Length - 1] = quaternionArray[FilteredCameraPositions.Length - 2];
        TrajectoryOrientation = quaternionArray;
    }

    private static void CreateTrajectoryCurvatures()
    {
        var cameraPositions = CameraPositions;
        var frameDistances = FrameDistances;
        var length = cameraPositions.Length;
        var numArray = new float[length];
        for (var index = 0; index < length; ++index)
        {
            var aP1 = cameraPositions[index];
            var num = frameDistances[index];
            var filteredCameraPosition1 = GetFilteredCameraPosition((float)GetFrameAtDistance(num - 10.0));
            var filteredCameraPosition2 = GetFilteredCameraPosition((float)GetFrameAtDistance(num + 10.0));
            Vector3 center;
            if (!CircleCenter(filteredCameraPosition1, aP1, filteredCameraPosition2, out center))
            {
                numArray[index] = 0.0f;
            }
            else
            {
                float magnitude = (center - filteredCameraPosition1).magnitude;
                Vector3 normalized = Vector3.Cross(aP1 - filteredCameraPosition1, Vector3.up).normalized;
                if (((double)center.x - (double)filteredCameraPosition1.x) * (double)normalized.x +
                    ((double)center.y - (double)filteredCameraPosition1.y) * (double)normalized.y +
                    ((double)center.z - (double)filteredCameraPosition1.z) * (double)normalized.z < 0.0)
                    magnitude *= -1f;
                numArray[index] = 1f / magnitude;
            }
        }

        for (var index1 = 0; index1 < 2; ++index1)
        {
            for (var index2 = 1; index2 < numArray.Length - 1; ++index2)
                numArray[index2] =
                    (float)(((double)numArray[index2 - 1] + (double)numArray[index2] + (double)numArray[index2 + 1]) /
                            3.0);
        }

        TrajectoryCurvatures = numArray;
    }

    private static Vector3 GetCameraPositionAt(int frame)
    {
        if (frame <= 0)
            return CameraPositions[0];
        return frame >= FrameCount ? CameraPositions[CameraPositions.Length - 1] : CameraPositions[frame];
    }

    private static Vector3 GetFilteredCameraPosition(float frame)
    {
        int index = (int)Math.Ceiling((double)frame);
        if (index <= 0)
            return FilteredCameraPositions[0];
        if (index >= FrameCount)
            return FilteredCameraPositions[FilteredCameraPositions.Length - 1];
        float t = frame - (float)(index - 1);
        return Vector3.Lerp(FilteredCameraPositions[index - 1], FilteredCameraPositions[index], t);
    }

    private static double GetFrameAtDistance(double distance)
    {
        double[] frameDistances = FrameDistances;
        int index = Array.BinarySearch<double>(frameDistances, distance);
        if (index < 0)
            index = ~index;
        if (index == 0)
            return 0.0;
        if (index == frameDistances.Length)
            return (double)(frameDistances.Length - 1);
        double num = InverseLerp(frameDistances[index - 1], frameDistances[index], distance);
        return (double)index + num - 1.0;
    }

    private static float GetLeftSideOffset(float frame)
    {
        if (!HasLeftSideOffsets)
            return DefaultLeftSideOffset;
        int index = Array.BinarySearch<int>(LeftSideOffsetFrames, (int)Math.Ceiling((double)frame));
        if (index < 0)
            index = ~index;
        if (index == 0)
            return LeftSideOffsets[0];
        if (index == LeftSideOffsetFrames.Length)
            return LeftSideOffsets[LeftSideOffsetFrames.Length - 1];
        float t = Mathf.InverseLerp((float)LeftSideOffsetFrames[index - 1], (float)LeftSideOffsetFrames[index], frame);
        return Mathf.Lerp(LeftSideOffsets[index - 1], LeftSideOffsets[index], t);
    }

    private static float GetRightSideOffset(float frame)
    {
        if (!HasRightSideOffsets)
            return DefaultRightSideOffset;
        int index = Array.BinarySearch<int>(RightSideOffsetFrames, (int)Math.Ceiling((double)frame));
        if (index < 0)
            index = ~index;
        if (index == 0)
            return RightSideOffsets[0];
        if (index == RightSideOffsetFrames.Length)
            return RightSideOffsets[RightSideOffsetFrames.Length - 1];
        float t = Mathf.InverseLerp((float)RightSideOffsetFrames[index - 1], (float)RightSideOffsetFrames[index],
            frame);
        return Mathf.Lerp(RightSideOffsets[index - 1], RightSideOffsets[index], t);
    }

    private static Quaternion GetTrajectoryOrientationAt(int frame)
    {
        int index = Mathf.Clamp(frame, 0, FrameCount);
        if (index <= 0)
            return TrajectoryOrientation[0];
        return index >= FrameCount
            ? TrajectoryOrientation[TrajectoryOrientation.Length - 1]
            : TrajectoryOrientation[index];
    }
    
    public static float GetTrajectoryCurvature(float frame)
    {
        var index = (int)Math.Ceiling((double)frame);
        if (index <= 0)
            return TrajectoryCurvatures[0];
        if (index >= FrameCount)
            return TrajectoryCurvatures[TrajectoryCurvatures.Length - 1];
        var t = frame - (float)(index - 1);
        return Mathf.Lerp(TrajectoryCurvatures[index - 1], TrajectoryCurvatures[index], t);
    }

    private static GameObject CreateTrajectory(int segment, float lOffset, float rOffset, float margin = float.NaN)
    {
        var start = 0;
        var end = CameraPositions.Length;
      
        var num1 = end - start + 1;
        var indices = new int[(num1 - 1) * 6];
        var vector3Array1 = new Vector3[num1 * 2];
        Vector3 left;
        Vector3 right;
        ARGameObjectFactory.CreateVertices(start, lOffset, rOffset, out left, out right, margin);
        vector3Array1[0] = left;
        vector3Array1[1] = right;
        var num2 = 2;
        var num3 = 0;
        for (int frame = start + 1; frame <= end; ++frame)
        {
            ARGameObjectFactory.CreateVertices(frame, lOffset, rOffset, out left, out right, margin);
            if (ARGameObjectFactory.IsValidVertex(left) && ARGameObjectFactory.IsValidVertex(right))
            {
                Vector3[] vector3Array2 = vector3Array1;
                var index1 = num2;
                var num4 = index1 + 1;
                
                var vector3_1 = left;
                vector3Array2[index1] = vector3_1;
                var vector3Array3 = vector3Array1;
                
                var index2 = num4;
                num2 = index2 + 1;
                
                var vector3_2 = right;
                vector3Array3[index2] = vector3_2;
                
                int[] numArray1 = indices;
                var index3 = num3;
                var num5 = index3 + 1;
                var num6 = num2 - 3;
                numArray1[index3] = num6;
                
                var numArray2 = indices;
                var index4 = num5;
                var num7 = index4 + 1;
                var num8 = num2 - 4;
                numArray2[index4] = num8;
                
                var numArray3 = indices;
                var index5 = num7;
                var num9 = index5 + 1;
                var num10 = num2 - 1;
                numArray3[index5] = num10;
                
                var numArray4 = indices;
                var index6 = num9;
                var num11 = index6 + 1;
                var num12 = num2 - 4;
                numArray4[index6] = num12;
                
                var numArray5 = indices;
                var index7 = num11;
                var num13 = index7 + 1;
                var num14 = num2 - 2;
                numArray5[index7] = num14;
                
                var numArray6 = indices;
                var index8 = num13;
                num3 = index8 + 1;
                var num15 = num2 - 1;
                numArray6[index8] = num15;
            }
        }

        var mesh = new Mesh
        {
            vertices = vector3Array1,
            indexFormat = IndexFormat.UInt32
        };
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        var trajectory = new GameObject(string.Format("Segment {0}", (object)segment), new System.Type[3]
        {
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(MeshCollider)
        });
        trajectory.GetComponent<MeshFilter>().sharedMesh = mesh;
        trajectory.GetComponent<MeshCollider>().sharedMesh = trajectory.GetComponent<MeshFilter>().sharedMesh;
        return trajectory;
    }

    private static void CreateVertices(int frame, float lOffset, float rOffset, out Vector3 left, out Vector3 right,
        float margin = float.NaN)
    {
        var trajectoryOrientationAt = GetTrajectoryOrientationAt(frame);
        var vector3 = GetCameraPositionAt(frame) - trajectoryOrientationAt * Vector3.up * CameraHeight;
        if ((double)lOffset == 0.0)
            lOffset = GetLeftSideOffset((float)frame);
        
        if (float.IsNaN(margin))
            lOffset += RouteMargin;
        else
            lOffset += margin;
        
        left = vector3 + trajectoryOrientationAt * Vector3.left * lOffset;
        if ((double)rOffset == 0.0)
            rOffset = GetRightSideOffset((float)frame);
        
        if (float.IsNaN(margin))
            rOffset += RouteMargin;
        else
            rOffset += margin;
        
        right = vector3 - trajectoryOrientationAt * Vector3.left * rOffset;
        left.y = right.y = vector3.y;
    }

    private static bool CircleCenter(Vector3 aP0, Vector3 aP1, Vector3 aP2, out Vector3 center)
    {
        var lhs = aP1 - aP0;
        var vector3 = aP2 - aP0;
        var rhs = Vector3.Cross(lhs, vector3);
        if ((double)rhs.sqrMagnitude < 9.9999998245167E-15)
        {
            center = Vector3.zero;
            return false;
        }

        rhs.Normalize();
        if ((double)rhs.sqrMagnitude < 9.9999998245167E-15)
        {
            center = Vector3.zero;
            return false;
        }

        var normalized1 = Vector3.Cross(lhs, rhs).normalized;
        var normalized2 = Vector3.Cross(vector3, rhs).normalized;
        var from = (lhs - vector3) * 0.5f;
        var num1 = Vector3.Angle(normalized1, normalized2);
        var num2 = Vector3.Angle(from, normalized1);
        var num3 = from.magnitude * Mathf.Sin(num2 * ((float)Math.PI / 180f)) /
                     Mathf.Sin(num1 * ((float)Math.PI / 180f));
        center = (double)Vector3.Dot(lhs, aP2 - aP1) <= 0.0
            ? aP0 + vector3 * 0.5f + normalized2 * num3
            : aP0 + vector3 * 0.5f - normalized2 * num3;
        return !float.IsInfinity(center.x) && !float.IsNaN(center.x) && !float.IsInfinity(center.y) &&
               !float.IsNaN(center.y) && !float.IsInfinity(center.z) && !float.IsNaN(center.z);
    }

    private static double InverseLerp(double a, double b, double value) => a != b ? (value - a) / (b - a) : 0.0;
}
