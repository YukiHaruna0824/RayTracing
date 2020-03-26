using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Camera cam;

    public UIManager um;

    public GameObject model;
    public GameObject cone;

    private List<GameObject> _lightObjects = new List<GameObject>();
    private List<GameObject> _objects = new List<GameObject>();


    private bool _hidePanel = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _hidePanel = !_hidePanel;
            um.gameObject.SetActive(_hidePanel);
        }
    }

    private void CreateLight(LightType type, Vector3 pos, Vector3 color , Vector3 whrInfo, int samples)
    {
        GameObject lightObj = new GameObject();
        lightObj.transform.position = pos;
        
        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.color = new Color(color.x, color.y, color.z) / 255.0f;

        if(type == LightType.point)
        {
            lightComp.type = UnityEngine.LightType.Point;
            lightObj.name = "PointLight";
            _lightObjects.Add(lightObj);
        }
        else if (type == LightType.area)
        {
            lightComp.type = UnityEngine.LightType.Area;
            lightObj.name = "AreaLight";
            lightComp.areaSize = new Vector2(whrInfo.x, whrInfo.y);
            _lightObjects.Add(lightObj);
        }
        else
            print("Create Light Object Failed!");
    }

    private void CreateObject(ObjectType otype, MaterialType mtype, Vector3 pos, Vector3 rot, Vector3 scale, Vector2 yminmax, Vector3 whrInfo, MaterialAttribute mattr)
    {
        if (otype == ObjectType.sphere)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.position = pos;
            obj.transform.eulerAngles = rot;
            //default radius 0.5
            float mult = whrInfo.z / 0.5f;
            obj.transform.localScale = scale * mult;
            _objects.Add(obj);

        }
        else if(otype == ObjectType.cylinder)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.position = pos;
            obj.transform.eulerAngles = rot;
            //delete default collider
            Destroy(obj.GetComponent<SphereCollider>());
            obj.AddComponent<MeshCollider>();
            obj.GetComponent<MeshCollider>().convex = true;
            //default yminmax +-1 radius 0.5
            float mult = whrInfo.z / 0.5f;
            obj.transform.localScale = new Vector3(scale.x * mult, scale.y * yminmax.y, scale.z * mult);
            _objects.Add(obj);
        }
        else if (otype == ObjectType.cone)
        {
            //default radius 1  height 2
            GameObject obj = Instantiate(cone, pos, Quaternion.Euler(rot));
            Vector3 mult = new Vector3(whrInfo.z, whrInfo.y / 2.0f, whrInfo.z);
            obj.transform.localScale = new Vector3(scale.x * mult.x, scale.y * mult.y, scale.z * mult.z);
            _objects.Add(obj);
        }
        else if (otype == ObjectType.plane)
        {
            //default size 10*10
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Vector3 mult = new Vector3(whrInfo.x / 10f, 1, whrInfo.y / 10f);
            obj.transform.position = pos;
            obj.transform.eulerAngles = rot;
            obj.transform.localScale = new Vector3(scale.x * mult.x, scale.y, scale.z * mult.z);
            _objects.Add(obj);
        }
        else if (otype == ObjectType.mesh)
        {
            /*string rootFolder = Directory.GetParent(mattr.meshName).FullName;
            loader.Load(rootFolder, Path.GetFileName(mattr.meshName));*/

            //blender object
            GameObject obj = Instantiate(model, pos, Quaternion.Euler(new Vector3(rot.x, 180, rot.z)));
            obj.transform.localScale = scale;
            _objects.Add(obj);
        }
    }

    public void ClearScene()
    {
        foreach(GameObject obj in _lightObjects)
            Destroy(obj);
        foreach (GameObject obj in _objects)
            Destroy(obj);
        _lightObjects.Clear();
        _objects.Clear();
    }

    public void GenerateScene(RayTracingInfo rtInfo, OutputInfo outputInfo)
    {
        //set screen resolution
        Screen.SetResolution((int)outputInfo.Resolution.x, (int)outputInfo.Resolution.y, false);

        //set camera info
        cam.transform.position = rtInfo.cameraPos;
        cam.transform.LookAt(rtInfo.cameraCenter, rtInfo.cameraUp);
        if (rtInfo.cameraType == 1) //default : perspective
            cam.orthographic = true;
        cam.fieldOfView = rtInfo.cameraFov;

        //create scene objects
        foreach(RayTracingObject robj in rtInfo.objects)
        {
            // light type
            if(robj.o_type == ObjectType.none)
                CreateLight(robj.l_type, robj.pos, robj.lightColor, robj.whrInfo, robj.nsample);

            // object type
            if (robj.l_type == LightType.none)
            {
                CreateObject(robj.o_type, robj.m_type, robj.pos, robj.rot, robj.scale, robj.yminmax, robj.whrInfo, robj.mattr);
            }

        }
    }

}
