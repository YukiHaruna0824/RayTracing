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

    //private List<GameObject> _lightObjects = new List<GameObject>();
    //private List<GameObject> _objects = new List<GameObject>();
    private Dictionary<GameObject, RayTracingObject> _lightObjects = new Dictionary<GameObject, RayTracingObject>();
    private Dictionary<GameObject, RayTracingObject> _objects = new Dictionary<GameObject, RayTracingObject>();


    private bool _hidePanel = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _hidePanel = !_hidePanel;
            um.gameObject.SetActive(_hidePanel);
        }
    }

    private void CreateLight(RayTracingObject robj)
    {
        GameObject lightObj = new GameObject();
        lightObj.transform.position = robj.pos;
        
        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.color = new Color(robj.lightColor.x, robj.lightColor.y, robj.lightColor.z) / 255.0f;

        if(robj.l_type == LightType.point)
        {
            lightComp.type = UnityEngine.LightType.Point;
            lightObj.name = "PointLight";
            _lightObjects[lightObj] = robj;
        }
        else if (robj.l_type == LightType.area)
        {
            lightComp.type = UnityEngine.LightType.Area;
            lightObj.name = "AreaLight";
            lightComp.areaSize = new Vector2(robj.whrInfo.x, robj.whrInfo.y);
            _lightObjects[lightObj] = robj;
        }
        else
            print("Create Light Object Failed!");
    }

    private Texture2D LoadImage(string filepath)
    {
        Texture2D tex = null;
        byte[] fileData;
        if (File.Exists(filepath))
        {
            fileData = File.ReadAllBytes(filepath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
        }
        else
            tex = Texture2D.blackTexture;
        return tex;
    }

    private void CreateObject(RayTracingObject robj)
    {
        GameObject obj = null;
        if (robj.o_type == ObjectType.sphere)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.position = robj.pos;
            obj.transform.eulerAngles = robj.rot;
            //default radius 0.5
            float mult = robj.whrInfo.z / 0.5f;
            obj.transform.localScale = robj.scale * mult;
        }
        else if (robj.o_type == ObjectType.cylinder)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.position = robj.pos;
            obj.transform.eulerAngles = robj.rot;
            //delete default collider
            Destroy(obj.GetComponent<SphereCollider>());
            obj.AddComponent<MeshCollider>();
            obj.GetComponent<MeshCollider>().convex = true;
            //default yminmax +-1 radius 0.5
            float mult = robj.whrInfo.z / 0.5f;
            obj.transform.localScale = new Vector3(robj.scale.x * mult, robj.scale.y * robj.yminmax.y, robj.scale.z * mult);
        }
        else if (robj.o_type == ObjectType.cone)
        {
            //default radius 1  height 2
            obj = Instantiate(cone, robj.pos, Quaternion.Euler(robj.rot));
            Vector3 mult = new Vector3(robj.whrInfo.z, robj.whrInfo.y / 2.0f, robj.whrInfo.z);
            obj.transform.localScale = new Vector3(robj.scale.x * mult.x, robj.scale.y * mult.y, robj.scale.z * mult.z);
        }
        else if (robj.o_type == ObjectType.plane)
        {
            //default size 10*10
            obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Vector3 mult = new Vector3(robj.whrInfo.x / 10f, 1, robj.whrInfo.y / 10f);
            obj.transform.position = robj.pos;
            obj.transform.eulerAngles = robj.rot;
            obj.transform.localScale = new Vector3(robj.scale.x * mult.x, robj.scale.y, robj.scale.z * mult.z);
        }
        else if (robj.o_type == ObjectType.mesh)
        {
            //string rootFolder = Directory.GetParent(mattr.meshName).FullName;
            //loader.Load(rootFolder, Path.GetFileName(mattr.meshName));

            //blender object
            obj = Instantiate(model, robj.pos, Quaternion.Euler(new Vector3(robj.rot.x, 180, robj.rot.z)));
            obj.transform.localScale = robj.scale;
        }

        if(robj.m_type == MaterialType.map)
        {
            Texture2D c_map = LoadImage(robj.mattr.cmapName);
            Texture2D b_map = LoadImage(robj.mattr.bmapName);
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetTexture("_MainTex", c_map);
            mat.SetTexture("_BumpMap", b_map);
            obj.GetComponent<MeshRenderer>().material = mat;
        }

        //bind raytracingInfo to GameObject
        _objects[obj] = robj;
    }

    //recursive get color
    private Color ShootRay(Ray r, float dis)
    {
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, 100))
        {

        }
        return new Color();
    }

    public void ClearScene()
    {
        foreach(GameObject obj in _lightObjects.Keys)
            Destroy(obj);
        foreach (GameObject obj in _objects.Keys)
            Destroy(obj);
        _lightObjects.Clear();
        _objects.Clear();
    }

    public void GenerateScene(RayTracingInfo rtInfo, OutputInfo outputInfo)
    {
        //set screen resolution (not work in editing)
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
                CreateLight(robj);

            // object type
            if (robj.l_type == LightType.none)
            {
                //CreateObject(robj.o_type, robj.m_type, robj.pos, robj.rot, robj.scale, robj.yminmax, robj.whrInfo, robj.mattr);
                CreateObject(robj);
            }               
        }
    }

    public void StartRender(RayTracingInfo rtInfo, OutputInfo outputInfo)
    {
        int sample = rtInfo.sampleCount;
        int sq_sample = (int)Mathf.Sqrt(sample);

        // for test
        Vector2 resol = new Vector2(20, 20);
        //Vector2 resol = outputInfo.Resolution;
        Vector2 scResol = new Vector2(Screen.width, Screen.height);

        Vector2 ratio = new Vector2(scResol.x / resol.x, scResol.y / resol.y);
        Vector2 s_ratio = new Vector2(ratio.x / sq_sample, ratio.y / sq_sample);

        float distance = 100f;

        for (int w = 0; w < resol.x; w++)
        {
            for (int h = 0; h < resol.y; h++)
            {
                Vector2 rayPoint = new Vector2(w * ratio.x, h * ratio.y);
                Color pixelColor = new Color(0, 0, 0);
                for (int sw = 0; sw < sq_sample; sw++)
                {
                    for (int sh = 0; sh < sq_sample; sh++)
                    {
                        Vector2 scRayPoint = new Vector2(rayPoint.x + sw * s_ratio.x, rayPoint.y + sh * s_ratio.y);
                        Ray ray = cam.ScreenPointToRay(scRayPoint);
                        // box filter
                        pixelColor += ShootRay(ray, distance) / sample;
                    }
                }
                // give current pixel color
                /*
                 
                */
            }
        }
    }
}
