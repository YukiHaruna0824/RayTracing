using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ObjLoader
{

    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector3> _normals = new List<Vector3>();
    private List<Vector2> _uv = new List<Vector2>();
    private List<Vector3> _faceIndex = new List<Vector3>();


    public string fileName;

    public Mesh ImportFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new Mesh();

        Parse(filePath);
        //Set Mesh
        Mesh mesh = new Mesh();

        int size = _faceIndex.Count;
        Vector3[] vertices = new Vector3[size];
        Vector2[] uv = new Vector2[size];
        Vector3[] normals = new Vector3[size];
        int[] triangles = new int[size];

        int index = 0;
        foreach(Vector3 v in _faceIndex)
        {
            vertices[index] = _vertices[(int)v.x - 1];
            if (v.y > 0)
                uv[index] = _uv[(int)v.y - 1];
            if (v.z > 0)
                normals[index] = _normals[(int)v.z - 1];

            triangles[index] = index;
            index++;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.normals = normals;
        mesh.triangles = triangles;

        return mesh;
    }

    private void Parse(string filepath)
    {
        fileName = Path.GetFileName(filepath);
        string line;
        StreamReader sr = new StreamReader(filepath);
        while((line = sr.ReadLine()) != null)
        {
            if(line.StartsWith("v ") || line.StartsWith("vt ") || line.StartsWith("f ") || line.StartsWith("vn "))
            {
                string[] tokens = line.Split(' ');
                if (tokens[0] == "v")
                    _vertices.Add(new Vector3(System.Convert.ToSingle(tokens[1]), System.Convert.ToSingle(tokens[2])
                        , System.Convert.ToSingle(tokens[3])));
                else if (tokens[0] == "vt")
                    _uv.Add(new Vector2(System.Convert.ToSingle(tokens[1]), System.Convert.ToSingle(tokens[2])));
                else if (tokens[0] == "vn")
                    _normals.Add(new Vector3(System.Convert.ToSingle(tokens[1]), System.Convert.ToSingle(tokens[2])
                        , System.Convert.ToSingle(tokens[3])));
                else if (tokens[0] == "f")
                {
                    for(int i = 1; i < 4; i++)
                    {
                        string[] token = tokens[i].Split('/');
                        // v/vt
                        if (token.Length == 2)
                            _faceIndex.Add(new Vector3(System.Convert.ToInt32(token[0]), System.Convert.ToInt32(token[1]), 0));
                        // v/vt/vn and v//vn
                        else if(token.Length == 3)
                        {
                            if(token[1] == "")
                                _faceIndex.Add(new Vector3(System.Convert.ToInt32(token[0]), 0
                                    , System.Convert.ToInt32(token[2])));
                            else
                                _faceIndex.Add(new Vector3(System.Convert.ToInt32(token[0]), System.Convert.ToInt32(token[1])
                                    , System.Convert.ToInt32(token[2])));
                        }
                    }
                }
            }
        }
    }
}
