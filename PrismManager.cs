using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;

    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism, bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }

    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions

    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        for (int i = 0; i < prisms.Count; i++)
        {
            for (int j = i + 1; j < prisms.Count; j++)
            {
                var checkPrisms = new PrismCollision();
                checkPrisms.a = prisms[i];
                checkPrisms.b = prisms[j];

                yield return checkPrisms;
            }
        }

        yield break;
    }
    //bool originally
    private Node CheckCollision(PrismCollision collision)
    {
        float tolerance = (float) Math.Pow(10,-5); //10 to the power of -5
        bool isCollision = false;
        Vector3 penetration_depth_vector = Vector3.zero;
        Prism prismA = collision.a;
        Prism prismB = collision.b;

        collision.penetrationDepthVectorAB = Vector3.zero;

        Vector3[] MKDiffPoints = MKDiff(prismA, prismB);
        List<Vector3> Simplex = new List<Vector3>();
        Vector3 w = MKDiffPoints[0];
        Simplex.Add(w);

        Vector3 v = -w;
        w = getSupportingPoint(MKDiffPoints, v);
        Simplex.Add(w);

        while (true)
        {
            Vector3 new_v = FindClosestPointFromOrigin(Simplex.ToArray()); //finds closest point to the origin from the Simplex

            if (Vector3.Distance(new_v, v) < tolerance)
            {
                break;
            }
            // Remove the third, irrelavant point from Simplex
            Simplex.RemoveAt(0);

            v = new_v;
            w = getSupportingPoint(MKDiffPoints, v);
            Simplex.Add(w);

        isCollision = DoesSimplexContainOrigin(Simplex);

            
        if (isCollision == false)
        {
            penetration_depth_vector = Vector3.zero;
        }
        else
        {
            List<Vector3> expandingPolygon = Simplex;
            Vector3 depth_vector = Vector3.zero;

            while (true)
            {
                Vector3 new_depth_vector = FindClosestPointFromOrigin(expandingPolygon.ToArray());
                if (Vector3.Distance(depth_vector, new_depth_vector) < tolerance)
                {
                    penetration_depth_vector = new_depth_vector;
                    break;
                }


                depth_vector = new_depth_vector;
                w = getSupportingPoint(MKDiffPoints, depth_vector);
                expandingPolygon.Add(w);
            }
        }
    }
        Node ans = new Node(isCollision, penetration_depth_vector);
        return ans ;
    }

   














    private 
    private Vector3[] MKDiff(Prism prismA, Prism prismB)
    {
        Vector3[] result = new Vector3[prismA.points.Length * prismB.points.Length];
        int k = 0;
        for (int i = 0; i < prismA.points.Length; i++)
        {
            for (int j = 0; j < prismB.points.Length; j++)
            {
                Vector3 point = new Vector3(prismA.points[i].x - prismB.points[j].x, prismA.points[i].y - prismB.points[j].y, prismA.points[i].z - prismB.points[j].z);
                result[k] = point;
                k++;
            }
        }
        return result;
    }

    

    private bool DoesSimplexContainOrigin(List<Vector3> Simplex)
    {
        Vector3 origin = new Vector3(0f, 0f, 0f);

        return Simplex.Contains(origin);
    }

    private static Vector3 Scale(float scale, Vector3 vec)
    {
        return new Vector3(scale*vec.x, scale*vec.y, scale*vec.z);
    }

    private static float Dot(Vector3 vec, Vector3 other)
    {
        return (vec.x*other.x) + (vec.y*other.y) + (vec.z*other.z);
    }

    private static Vector3 Projection(Vector3 vec, Vector3 other)
    {
        return Scale(Dot(vec, other) / Dot(other, other), other);
    }



    private Vector3 getSupportingPoint(Vector3[] MKDiffPoints, Vector3 v)
    {
        float[] distance = new float[MKDiffPoints.Length];
        for (int i = 0; i < MKDiffPoints.Length; i++)
        {
            distance[i] = distanceFromOrigin(Projection(MKDiffPoints[i], v));
        }
        for (int i = 1; i < MKDiffPoints.Length; i++)
        {
            float key = distance[i];
            Vector3 temp = MKDiffPoints[i];
            int j = i - 1;

            while (j >= 0 && distance[j] > key)
            {
                distance[j + 1] = distance[j];
                MKDiffPoints[j+1] = MKDiffPoints[j+1];
                j = j - 1;
            }
            distance[j + 1] = key;
            MKDiffPoints[j+1] = temp;
        }

        return MKDiffPoints[0];
    }


    private Vector3 FindClosestPointFromOrigin(Vector3[] Simplex)
    {
        float min = float.MaxValue;
        float distance = 0f;
        Vector3 result = new Vector3(0, 0, 0);
        for (int i = 0; i < Simplex.Length; i++)
        {
            distance = distanceFromOrigin(Simplex[i]);
            if (distance < min)
            {
                result = Simplex[i];
            }
        }
        return result;
    }

    private float distanceFromOrigin(Vector3 vec)
    {
        float distance = (float) Math.Sqrt(Math.Pow(vec.x, 2) + Math.Pow(vec.y, 2) + Math.Pow(vec.z, 2));
        return distance;
    }

    private bool DoesSimplexContainOrigin(Vector3[] simplex)
    {
        bool result = false;
        for (int i = 0; i < simplex.Length; i++)
        {
            if (simplex[i].x == 0 && simplex[i].y == 0 && simplex[i].z == 0)
            {
                result = true;
            }
        }
        return result;
    }
    
    #endregion

    #region Private Functions

    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        for (int i = 0; i < collision.a.pointCount; i++)
        {
            collision.a.points[i] += pushA;
        }
        for (int i = 0; i < collision.b.pointCount; i++)
        {
            collision.b.points[i] += pushB;
        }
        //prismObjA.transform.position += pushA;
        //prismObjB.transform.position += pushB;

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }

    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();

        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion

    #region Utility Classes

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K, V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v)
        {
            Item1 = k;
            Item2 = v;
        }
    }

    private class Node
    {
        public bool a; //isCollision
        public Vector3 b; //penetration depth vector

        public Node(bool a, Vector3 b)
        {
            this.a = a;
            this.b = b;
            
        }
    }
    #endregion
}
