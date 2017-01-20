using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
public class Point3DRGB {
	public Vector3 point;
	public Color color;
	
	public Point3DRGB(Vector3 point, Color color){
		this.point = point;
		this.color = color;
	}
}

public class PointCloudSimple : MonoBehaviour {
    Mesh[] highres_cloud;
    Mesh[] lowres_cloud;
    int highres_nclouds = 0;
    int lowres_nclouds = 0;
    int id;

    List<Vector3> pointsH;
    List<int> indH;
    List<Color> colorsH;

    List<Vector3> pointsL;
    List<int> indL;
    List<Color> colorsL;

    int pointCount;
    int l;
    int h;
    Vector3[] posBucket;
    Color[] colBucket;
    byte[] buffer;
  

    [StructLayout(LayoutKind.Explicit)]
    struct UnionArray
    {
        [FieldOffset(0)]
        public byte[] Bytes;

        [FieldOffset(0)]
        public float[] Floats;

    }
    int countPack = 0;
    public void setPoints(byte[] receivedBytes, int step,int newid){

        //pointsH.Clear();
        //colorsH.Clear();
        //indH.Clear();
        //pointsL.Clear();
        //indL.Clear();
        //colorsL.Clear();

        pointsH = new List<Vector3>();
        colorsH = new List<Color>();
        indH = new List<int>();
        pointsL = new List<Vector3>();
        indL = new List<int>();
        colorsL = new List<Color>();
 
        UnionArray rec = new UnionArray { Bytes = receivedBytes };
     
        if (newid > id) {
            id = newid;

            //for(int a = 0; a < 4; a++) {
            //    lowres_cloud[a].Clear();
            //    highres_cloud[a].Clear();
            //}

            for (int a = 0; a < 4; a++)
            {
                lowres_cloud[a] = new Mesh();
                highres_cloud[a] = new Mesh();
            }

            highres_nclouds = 0;
            lowres_nclouds = 0;
            l = 0;
            h = 0;
            pointCount = 0;
            countPack = 0;
        }
        else if(newid == id)
        {
            countPack++;
            pointsL.AddRange(lowres_cloud[lowres_nclouds].vertices);
            colorsL.AddRange(lowres_cloud[lowres_nclouds].colors);
            indL.AddRange(lowres_cloud[lowres_nclouds].GetIndices(0));

            pointsH.AddRange(highres_cloud[highres_nclouds].vertices);
            colorsH.AddRange((highres_cloud[highres_nclouds].colors));
            indH.AddRange(highres_cloud[highres_nclouds].GetIndices(0));

            l = pointsL.Count;
            h = pointsH.Count;

        }else
        {
            Debug.Log("Old packet");
            return;
        }

        float x, y, z;
        byte r, g, b;
        
        for (int i = step; i < receivedBytes.Length; i += 16) // Each point is represented by 16 bytes.
        {
            try
            {
                if (i + 15 > receivedBytes.Length) break; // Insurance.
                int floatindex = (int) (i / 4.0);
                x = rec.Floats[floatindex];
                y = rec.Floats[floatindex + 1];
                z = rec.Floats[floatindex + 2];
                //buffer[0] = receivedBytes[i];
                //buffer[1] = receivedBytes[i + 1];
                //buffer[2] = receivedBytes[i + 2];
                //buffer[3] = receivedBytes[i + 3];
                //x = System.BitConverter.ToSingle(buffer, 0); // x

                //buffer[0] = receivedBytes[i + 4];
                //buffer[1] = receivedBytes[i + 5];
                //buffer[2] = receivedBytes[i + 6];
                //buffer[3] = receivedBytes[i + 7];
                //y = System.BitConverter.ToSingle(buffer, 0); // y

                //buffer[0] = receivedBytes[i + 8];
                //buffer[1] = receivedBytes[i + 9];
                //buffer[2] = receivedBytes[i + 10];
                //buffer[3] = receivedBytes[i + 11];
                //z = System.BitConverter.ToSingle(buffer, 0); // z

                r = receivedBytes[i + 12]; // r
                g = receivedBytes[i + 13]; // g
                b = receivedBytes[i + 14]; // b

                //   Point3DRGB pt = new Point3DRGB(new Vector3(x, y, z), new Color((float)r / 255, (float)g / 255, (float)b / 255));
                Vector3 pos = posBucket[pointCount];
                pos.Set(x,y,z);
                Color c = colBucket[pointCount++];
                c.r = (float)r / 255;
                c.g = (float)g / 255;
                c.b = (float)b / 255;

                 if (receivedBytes[i + 15] == 1)// If it's a HR point, save it to the high resolution points.
                {
                    pointsH.Add(pos);
                    colorsH.Add(c);
                    indH.Add(h++);
                }
                else
                {
                    pointsL.Add(pos);
                    colorsL.Add(c);
                    indL.Add(l++);
                }

                if(h == 65000)
                {
                    highres_cloud[highres_nclouds].vertices = pointsH.ToArray();
                    highres_cloud[highres_nclouds].colors = colorsH.ToArray();
                    highres_cloud[highres_nclouds].SetIndices(indH.ToArray(), MeshTopology.Points, 0);

                    h = 0;
                    //pointsH.Clear();
                    //colorsH.Clear();
                    //indH.Clear();
                    pointsH = new List<Vector3>();
                    colorsH = new List<Color>();
                    indH = new List<int>();
                    highres_nclouds++; 
                }

                if (l == 65000)
                {
                    lowres_cloud[lowres_nclouds].vertices = pointsL.ToArray();
                    lowres_cloud[lowres_nclouds].colors = colorsL.ToArray();
                    lowres_cloud[lowres_nclouds].SetIndices(indL.ToArray(), MeshTopology.Points, 0);

                    l = 0;
                    //pointsL.Clear();
                    //colorsL.Clear();
                    //indL.Clear();
                  
                    pointsL = new List<Vector3>();
                    indL = new List<int>();
                    colorsL = new List<Color>();
                    lowres_nclouds++;
                }
            }
            catch (System.Exception exc)
            {
                Debug.Log("Reached out of the array: " + exc.StackTrace);
            }
        }

        highres_cloud[highres_nclouds].vertices = pointsH.ToArray();
        highres_cloud[highres_nclouds].colors = colorsH.ToArray();
        highres_cloud[highres_nclouds].SetIndices(indH.ToArray(), MeshTopology.Points, 0);

        lowres_cloud[lowres_nclouds].vertices = pointsL.ToArray();
        lowres_cloud[lowres_nclouds].colors = colorsL.ToArray();
        lowres_cloud[lowres_nclouds].SetIndices(indL.ToArray(), MeshTopology.Points, 0);
    }

	public void setToView(){
		MeshFilter[] filters = GetComponentsInChildren<MeshFilter> ();
        // Note that there are 8 MeshFilter -> [HR HR HR HR LR LR LR LR]
        int lr =lowres_nclouds + 4;  // Therefore, the low resolution clouds start at index 4
        for (int i = 0; i < filters.Length; i++) {
            MeshFilter mf = filters[i];
            if (i <= highres_nclouds)
            {
				mf.mesh = highres_cloud[i];
            }
            else if (i <= lr && i >= 4)
            {
                mf.mesh = lowres_cloud[i - 4];
            }
            else
            {
				mf.mesh.Clear();
            }
		}
    }

	public void hideFromView(){
		MeshFilter[] filters = GetComponentsInChildren<MeshFilter> ();
		foreach (MeshFilter mf in filters) {
			mf.mesh.Clear();
		}	
	}

    // Use this for initialization
    void Start() {
        // Material for the high resolution points
        Material mat = Resources.Load("Materials/cloudmat") as Material;
        // Material for the low resolution points
        Material other = Instantiate(mat) as Material;

        // Update size for each material.
        mat.SetFloat("_Size", 3);  // HR
        other.SetFloat("_Size", 5); // LR

        for (int i = 0; i < 4; i++) {
            GameObject a = new GameObject("highres_cloud" + i);
            MeshFilter mf = a.AddComponent<MeshFilter>();
            MeshRenderer mr = a.AddComponent<MeshRenderer>();
            mr.material = mat;
            a.transform.parent = this.gameObject.transform;
            a.transform.localPosition = Vector3.zero;
            a.transform.localRotation = Quaternion.identity;
            a.transform.localScale = new Vector3(1, 1, 1);
        }
        for (int i = 0; i < 4; i++)
        {
            GameObject a = new GameObject("lowres_cloud" + i);
            MeshFilter mf = a.AddComponent<MeshFilter>();
            MeshRenderer mr = a.AddComponent<MeshRenderer>();
            mr.material = other;
            a.transform.parent = this.gameObject.transform;
            a.transform.localPosition = Vector3.zero;
            a.transform.localRotation = Quaternion.identity;
            a.transform.localScale = new Vector3(1, 1, 1);
        }
        pointsH = new List<Vector3>();
        colorsH = new List<Color>();
        indH = new List<int>();
        pointsL = new List<Vector3>();
        indL = new List<int>();
        colorsL = new List<Color>();
        highres_cloud = new Mesh[4];
        lowres_cloud = new Mesh[4];
        for (int a = 0; a < 4; a++) {
            lowres_cloud[a] = new Mesh();
            highres_cloud[a] = new Mesh();
        }
        colBucket = new Color[217088];
        posBucket = new Vector3[217088];
        for (int i = 0; i < 217088; i++)
        {
            posBucket[i] = new Vector3();
            colBucket[i] = new Color();
        }

        buffer = new byte[4]; // Buffer for the x, y and z floats
        pointCount = 0;
        l = 0;
        h = 0;
    }

}