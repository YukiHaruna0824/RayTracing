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

    Texture2D tex;
    bool displayTex = true;
    Rect rect = new Rect(0, 0, 0, 0);

    private bool _hidePanel = true;

    float[] depthValue = new float[] { 0.96f, 0.03f, 0.0003f,0.0003f };

    byte[] matIndex;

    private Color ToColor(Vector3 v)
    {
        return new Color(v.x, v.y, v.z);
    }

    private Vector3 ToVector(Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }

    private void OnGUI()
    {
        if (displayTex)
            if (rect.width != 0 && rect.height != 0)
                GUI.DrawTexture(rect, tex);
    }

    public void ToggleImage()
    {
        displayTex = !displayTex;
    }

    private void Update()
    {
        if (rect.width != 0 && rect.height != 0)
            tex.Apply();
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
        lightComp.color = new Color(robj.lightColor.x, robj.lightColor.y, robj.lightColor.z);

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
            //Special Get Child
            obj = obj.transform.GetChild(0).gameObject;
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
            //Special Get Child
            obj = obj.transform.GetChild(0).gameObject;
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

    public void ClearScene()
    {
        foreach(GameObject obj in _lightObjects.Keys)
            Destroy(obj);
        foreach (GameObject obj in _objects.Keys)
        {
            Destroy(obj);
        }
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
                CreateObject(robj);
        }

    }

    float determineAttenuation(float light_dist)
    {
        float attenuation = (Mathf.Pow(light_dist, -2f));
        attenuation *= 10f;
        if (attenuation > 1)
            attenuation = 1;
        return attenuation;
    }

    Vector3 getModelColor(ref RaycastHit hit)
    {
        int triangleIdx = hit.triangleIndex;

        Material[] mats = hit.transform.GetComponent<MeshRenderer>().sharedMaterials;
        Vector3 mapColor = new Vector3(0, 0, 0);
        Texture2D matTex = mats[matIndex[triangleIdx]].mainTexture as Texture2D;
        if (matTex != null)
        {
            mapColor = ToVector(matTex.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y) * mats[matIndex[triangleIdx]].color);
        }
        else if (mats[matIndex[triangleIdx]].color != null)
            mapColor = ToVector(mats[matIndex[triangleIdx]].color);
        return mapColor;
    }

    bool isInShadow(Vector3 light_pos,ref RaycastHit hit)
    {
        Vector3 lv = hit.point - light_pos;
        if (Vector3.Dot(-lv, hit.normal) < 0)
            return false;

        Ray shadowRay = new Ray(light_pos, lv);

        RaycastHit shadowHit;
        if (Physics.Raycast(shadowRay, out shadowHit, lv.magnitude * 0.9999f))
        {
            if (_objects[shadowHit.transform.gameObject].m_type != MaterialType.glass)
                //Debug.Log(shadowHit.transform.gameObject.name);
                return true;
        }
        return false;
    }

    private Vector3[] SampleLightPoint(RayTracingObject robj)
    {
        Vector3[] points = null;
        if (robj.l_type == LightType.point)
        {
            points = new Vector3[1];
            points[0] = robj.pos;
        }
        else if (robj.l_type == LightType.area)
        {
            float width = robj.whrInfo.x;  //x axis
            float height = robj.whrInfo.y; //y axis

            int sample = robj.nsample;
            points = new Vector3[sample * sample];

            Vector3 leftTopPos = new Vector3(robj.pos.x - 0.5f * width, robj.pos.y - 0.5f * height, robj.pos.z);
            Vector2 ratio = new Vector2(width / sample, height / sample);
            for (int w = 0; w < sample; w++)
                for (int h = 0; h < sample; h++)
                    points[w * sample + h] = new Vector3(leftTopPos.x + ratio.x * (w + 0.5f), leftTopPos.y + ratio.y * (h + 0.5f), robj.pos.z);
        }

        return points;
    }

    
    private void CheckShadowRay(ref Ray ray, ref RaycastHit hit, ref Color albedo)
    {
        foreach (GameObject lObj in _lightObjects.Keys)
        {
            RayTracingObject lightObj = _lightObjects[lObj];
            RayTracingObject hitObject = _objects[hit.transform.gameObject];
            Vector3[] lightSamplePoint = SampleLightPoint(lightObj);

            foreach(Vector3 lightPos in lightSamplePoint)
            {
                if(!isInShadow(lightPos, ref hit))
                {
                    Vector3 light = lightPos - hit.point;
                    Vector3 light_norm = Vector3.Normalize(light);

                    Vector3 halfDir = Vector3.Normalize(-ray.direction + light_norm);
                    float diffuse_scalar = Mathf.Max(0, Vector3.Dot(light_norm, hit.normal));
                    float specular_scalar = Mathf.Pow(Mathf.Max(0, Vector3.Dot(halfDir, hit.normal)), 6);

                    if(hitObject.m_type == MaterialType.none)
                    {
                        Vector3 mapColor = getModelColor(ref hit);
                        albedo += ToColor(lightObj.lightColor) * ToColor(diffuse_scalar * mapColor);
                    }
                    else if(hitObject.m_type == MaterialType.map)
                    {
                        Texture2D tex = hit.transform.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
                        Vector2 pixelUV = hit.textureCoord;
                        Vector3 mapColor = ToVector(tex.GetPixel((int)(pixelUV.x * tex.width), (int)(pixelUV.y * tex.height)));
                        albedo += ToColor(lightObj.lightColor) * ToColor((diffuse_scalar * mapColor));
                    }
                    else if (hitObject.m_type == MaterialType.kdks)
                    {
                        albedo += ToColor(lightObj.lightColor) * ToColor(diffuse_scalar * hitObject.mattr.kd + specular_scalar * hitObject.mattr.ks);
                    }
                }
            }
            albedo /= lightSamplePoint.Length;
        }
    }


    public bool Refract(Vector3 v, Vector3 n, float ni_over_nt, out Vector3 outRefracted)
    {
        float dt = Vector3.Dot(-v.normalized, n.normalized);
        float discrement = 1.0f - ni_over_nt * ni_over_nt * (1.0f - dt * dt);
        if (discrement > 0)
        {
            outRefracted = ni_over_nt * (v.normalized - n * dt) - n * Mathf.Sqrt(discrement);
            return true;
        }
        outRefracted = new Vector3(0, 0, 0);
        return false;
    }

    public float Schlick(float cosine, float ri)
    {
        float r0 = (1 - ri) / (1 + ri);
        r0 = r0 * r0;
        return r0 + (1 - r0) * Mathf.Pow(1 - cosine, 5);
    }

    private bool Scatter(ref Ray ray, ref RaycastHit hit, ref Color albedo, ref Ray scattered,ref int depth,ref int reflCount,ref bool RayFromGlass,ref Collider GlassCollider)
    {

        RayTracingObject hitObject = _objects[hit.transform.gameObject];
        if (hitObject.m_type == MaterialType.mirror)
        {
            Vector3 refl = Vector3.Reflect(ray.direction, hit.normal);

            scattered.origin = hit.point;
            scattered.direction = refl;
            albedo = new Color(0, 0, 0);
            return Vector3.Dot(scattered.direction, hit.normal) > 0;
        }
        else if (hitObject.m_type == MaterialType.glass)
        {
            float refractRate = 1.5f;
            Vector3 outwardNormal;
            Vector3 rdir = ray.direction.normalized;
            Vector3 reflect = Vector3.Reflect(rdir.normalized, hit.normal.normalized);
            float ni_over_nt;
            albedo = new Color(0, 0, 0);
            Vector3 refract;
            float reflectProbe;
            float cosine;
            if (Vector3.Dot(rdir, hit.normal) > 0)
            {
                outwardNormal = -hit.normal.normalized;
                ni_over_nt = refractRate;
                cosine = refractRate * Vector3.Dot(rdir, hit.normal.normalized);
            }
            else
            {
                outwardNormal = hit.normal.normalized;
                ni_over_nt = 1.0f / refractRate;
                cosine = -Vector3.Dot(rdir, hit.normal.normalized);
            }

            if (Refract(rdir, outwardNormal, ni_over_nt, out refract))
            {
                reflectProbe = Schlick(cosine, refractRate);
            }
            else
            {
                reflectProbe = 1;
            }

            if (Random.Range(0, 1) < reflectProbe)
            {
                scattered = new Ray(hit.point, reflect.normalized);
            }
            else
            {
                if (RayFromGlass == false)
                {
                    scattered = new Ray(hit.point, refract.normalized);
                    GlassCollider = hit.transform.gameObject.GetComponent<Collider>();
                    RayFromGlass = true;
                }
                else if (RayFromGlass == true) 
                {
                    scattered = new Ray(hit.point, refract.normalized);
                    GlassCollider = null;
                    RayFromGlass = false;
                }
            }
            return true;
        }
        else if (hitObject.m_type == MaterialType.none) 
        {
            Vector3 target = hit.normal.normalized * 0.5f +
            new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            scattered.origin = hit.point;
            scattered.direction = target - hit.point;

            CheckShadowRay(ref ray, ref hit, ref albedo);
            albedo *= depthValue[reflCount++];

            return true;
        }
        else if(hitObject.m_type == MaterialType.map)
        {

            Vector3 target = hit.normal.normalized * 0.5f +
            new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            scattered.origin = hit.point;
            scattered.direction = target - hit.point;

            CheckShadowRay(ref ray, ref hit, ref albedo);
            albedo *= depthValue[reflCount++];

            return true;
        }
        else if (hitObject.m_type == MaterialType.kdks)
        {

            Vector3 target = hit.normal.normalized * 0.5f +
            new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            scattered.origin = hit.point;
            scattered.direction = target - hit.point;

            CheckShadowRay(ref ray, ref hit, ref albedo);
            albedo *= depthValue[reflCount++];

            return true;
        }
        return false;
    }

    //recursive get color
    private Color RayTrace(Ray ray, int depth,ref int reflCount,ref bool RayFromGlass,ref Collider GlassCollider)
    {
        RaycastHit hit;
        bool rayHit = false;
        
        if (RayFromGlass == false)
        {
            rayHit = Physics.Raycast(ray, out hit, 100);
        }
        else
        {
            Vector3 Origin = ray.origin;
            ray.origin = ray.GetPoint(80);
            ray.direction = -ray.direction;
            rayHit = GlassCollider.Raycast(ray, out hit, 100);
            ray.direction = -ray.direction;
            ray.origin = Origin;
        }

        if (rayHit)
        {
            Ray scattered = new Ray();
            Color albedo = new Color(0,0,0);

            if (Scatter(ref ray, ref hit, ref albedo, ref scattered,ref depth,ref reflCount,ref RayFromGlass, ref GlassCollider)) 
            {
                if (depth < 2)
                    return albedo + RayTrace(scattered, depth + 1,ref reflCount,ref RayFromGlass,ref GlassCollider);
                else
                    return albedo;
            }
            else
            {
                return new Color(0.0f, 0.0f, 0.0f);
            }
        }
        else
        {
            Vector3 unitDir = ray.direction.normalized;
            float t = 0.5f * (unitDir.y + 1.0f);
            return ((1.0f - t) * new Color(1.0f, 1.0f, 1.0f) + t * new Color(0.5f, 0.7f, 1.0f)) * 0.3f;
        }

        //return new Color(0.5f, 0.9f, 1.0f);
    }

    public void PreprocessModelMaterial()
    {
        foreach (GameObject obj in _objects.Keys)
        {
            RayTracingObject rObj = _objects[obj];

            if (rObj.m_type == MaterialType.none)
            {
                Mesh mesh = obj.transform.GetComponent<MeshFilter>().mesh;
                matIndex = new byte[mesh.triangles.Length / 3];
                int subMeshesNr = mesh.subMeshCount;

                for (int k = 0; k < mesh.triangles.Length / 3; k++)
                {
                    int materialIdx = -1;
                    int[] hittedTriangle = new int[] { mesh.triangles[k * 3], mesh.triangles[k * 3 + 1], mesh.triangles[k * 3 + 2] };

                    for (int i = 0; i < subMeshesNr; i++)
                    {
                        int[] tr = mesh.GetTriangles(i);
                        for (int j = 0; j < tr.Length - 2; j++)
                        {
                            if (tr[j] == hittedTriangle[0] && tr[j + 1] == hittedTriangle[1] && tr[j + 2] == hittedTriangle[2])
                            {
                                materialIdx = i;
                                break;
                            }
                        }
                        if (materialIdx != -1) break;
                    }
                    matIndex[k] = (byte)materialIdx;
                }
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

        rect = new Rect(0, 0, resol.x, resol.y);
        tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
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
                        int reflCount = 0;
                        Vector2 scRayPoint = new Vector2(rayPoint.x + sw * s_ratio.x, rayPoint.y + sh * s_ratio.y);
                        Ray ray = cam.ScreenPointToRay(scRayPoint);
                        // box filter
                        bool RayFromGlass = false;
                        Collider GlassCollider=null;
                        pixelColor += RayTrace(ray, 0, ref reflCount,ref RayFromGlass,ref GlassCollider) / sample;
                    }
                }
                TextureHelper.SetPixel(tex, w, h, pixelColor);
            }
        }
        TextureHelper.SaveImg(tex, "Img/output.png");
    }

    public void StartRenderRect(RayTracingInfo rtInfo, OutputInfo outputInfo)
    {
        Debug.Log("Started!");
        matIndex = File.ReadAllBytes(Path.Combine(Application.dataPath, "ModelData.bin"));
        //PreprocessModelMaterial();
        //File.WriteAllBytes(Path.Combine(Application.dataPath, "ModelData.bin"), matIndex);
        Debug.Log("Model Preprocessed!");

        int sample = rtInfo.sampleCount;
        int sq_sample = (int)Mathf.Sqrt(sample);

        int width = 800;
        int height = 800;
        rect = new Rect((800 - width) / 2, (800 - height) / 2, width, height);

        Vector2 s_ratio = new Vector2(1f / sq_sample, 1f / sq_sample);

        tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);

        int total = (int)(rect.width * rect.height);
        for (int w = (int)rect.x; w < (int)rect.x + rect.width; w++)
        {
            for (int h = (int)rect.y; h < (int)rect.y + rect.height; h++)
            {
                Vector2 rayPoint = new Vector2(w, h);
                Color pixelColor = new Color(0, 0, 0);

                for (int sw = 0; sw < sq_sample; sw++)
                {
                    for (int sh = 0; sh < sq_sample; sh++)
                    {
                        int reflCount = 0;
                        Vector2 scRayPoint = new Vector2(rayPoint.x + sw * s_ratio.x, rayPoint.y + sh * s_ratio.y);
                        Ray ray = cam.ScreenPointToRay(scRayPoint);
                        // box filter
                        bool RayFromGlass = false;
                        Collider GlassCollider = null;
                        pixelColor += RayTrace(ray, 0, ref reflCount, ref RayFromGlass, ref GlassCollider) / sample;
                    }
                }
                TextureHelper.SetPixel(tex, w - (int)rect.x, h - (int)rect.y, pixelColor);
            }
        }
        Debug.Log("Finished!");

        TextureHelper.SaveImg(tex, "Img/output.png");
    }
}
