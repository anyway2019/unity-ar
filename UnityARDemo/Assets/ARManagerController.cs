using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Video;

public class ARManagerController : MonoBehaviour
{
    public Camera ARCamera;
    
    public Material TrajectoryMaterial;

    public VideoPlayer ARPlayer;

    private const float _cameraHeight = 1.3f;
    
    public float Leftoffset;
    
    public float RightOffset;

    private List<Vector3> _positions = new List<Vector3>();

    private List<Vector3> _eulerRotations = new List<Vector3>();
    
    private List<Quaternion> _rotations = new List<Quaternion>();
    
    // Start is called before the first frame update
    void Start()
    {
        var demo = Resources.Load<TextAsset>("demo");
        ParseXmlCamera(demo.text);
        RenderVirtualTrajectory();
    }

    // Update is called once per frame
    void Update()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        var currentFrame = (int)ARPlayer.frame;
        if (currentFrame < 0 || currentFrame > _positions.Count) return;
        ARCamera.transform.position = _positions[currentFrame];
        ARCamera.transform.rotation = _rotations[currentFrame];
    }

    /// <summary>
    /// get camera path(the left hand coordinate) by parsing the XMLCamera file.
    /// </summary>
    /// <param name="xml"></param>
    private void ParseXmlCamera(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
  
        var positionNodeList = document.SelectNodes("xml/pfcamera/translation");
        if (positionNodeList == null) return;
        foreach (XmlNode pos in positionNodeList)
        {
            var arr = pos.InnerText.Split(' ');
            var originalPos = new Vector3(arr[0].ToFloat(), arr[1].ToFloat(), arr[2].ToFloat());
            _positions.Add(originalPos.ReversePosition());
        }

        var rotationNodeList = document.SelectNodes("xml/pfcamera/rotation");
        if (rotationNodeList == null) return;
        foreach (XmlNode rotation in rotationNodeList)
        {
            var arr = rotation.InnerText.Split(' ');
            var originalRotation = new Vector3(arr[0].ToFloat(), arr[1].ToFloat(), arr[2].ToFloat());
            _eulerRotations.Add(originalRotation.ReverseRotation().eulerAngles);
            _rotations.Add(originalRotation.ReverseRotation());
        }
    }

    /// <summary>
    /// render trajectory over the movie texture.
    /// </summary>
    private void RenderVirtualTrajectory()
    {
        ARGameObjectFactory.CameraHeight = _cameraHeight;
        ARGameObjectFactory.Init(_positions.ToArray(),_eulerRotations.ToArray(),Leftoffset,RightOffset,TrajectoryMaterial);
    }
}
