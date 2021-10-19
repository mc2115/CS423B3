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
        UnityEngine.Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + UnityEngine.Random.value * 7);
            var randYRot = UnityEngine.Random.value * 360;
            var randScale = new Vector3((UnityEngine.Random.value - 0.5f) * 2 * maxPrismScaleXZ, (UnityEngine.Random.value - 0.5f) * 2 * maxPrismScaleY, (UnityEngine.Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((UnityEngine.Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (UnityEngine.Random.value - 0.5f) * 2 * prismRegionRadiusY, (UnityEngine.Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (UnityEngine.Random.value < 0.5f)
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

    private IEnumerable<PrismCollision> PotentialCollisions(){
       Dictionary <Vector3, Prism> dict = new Dictionary <Vector3, Prism> ();
       List <Vector3> points = new List <Vector3>();
       for (int i = 0; i < prisms.Count; i++) {
           float[] temp=minMaxXY(prisms[i]);
           Vector3 min1 = new Vector3(temp[0], temp[1], temp[2]);
           Vector3 max1 = new Vector3(temp[3], temp[4], temp[5]);
           Vector3 min2 = new Vector3(temp[0], temp[4], temp[2]);
           Vector3 max2 = new Vector3(temp[3], temp[1], temp[5]);
           Vector3 min3 = new Vector3(temp[0], temp[1], temp[5]);
           Vector3 max3 = new Vector3(temp[3], temp[4], temp[2]);
           Vector3 min4 = new Vector3(temp[0], temp[4], temp[5]);
           Vector3 max4 = new Vector3(temp[3], temp[1], temp[2]);
           Prism val=prisms[i];
           dict[min1] = val;
           dict[max1] = val;
           dict[min2] = val;
           dict[max2] = val;
           dict[min3] = val;
           dict[max3] = val;
           dict[min4] = val;
           dict[max4] = val;
           points.Add(min1);
           points.Add(max1);
           points.Add(min2);
           points.Add(max2);
           points.Add(min3);
           points.Add(max3);
           points.Add(min4);
           points.Add(max4);
      }
      KDTree kd = new KDTree(points, 0);
      var collisions = new List <PrismCollision> ();
      var activeList = new List <Prism> ();
      traverseTree(kd, activeList, dict, collisions);
      for(int i = 0; i < collisions.Count; i++){
          yield return collisions[i];
      }
   }

   private void traverseTree(KDTree root, List <Prism> activeList, Dictionary <Vector3, Prism> dict, List<PrismCollision> collisions){
       if (root == null){
           return;
       }
       if(root.leftChild != null){
           traverseTree(root.leftChild, activeList, dict, collisions);
       }
       Prism p = dict[root.location];
       if(activeList.Contains(p)){
           int index = activeList.IndexOf(p);
           for (int j = index+1; j < activeList.Count; j++){
               PrismCollision coll = new PrismCollision();
               coll.a=p;
               coll.b=activeList[j];
               collisions.Add(coll);
           }
       } else{
           activeList.Add(p);
       }
       if(root.rightChild != null){
           traverseTree(root.rightChild, activeList, dict, collisions);
       }
   }
    private bool collEquals(PrismCollision colX, PrismCollision colY){
      return ((colX.a==colY.a && colX.b==colY.b)||(colX.a==colY.b && colX.b==colY.a));
    }
    private float[] minMaxXY(Prism p){
      float minX=int.MaxValue;
      float minY=int.MaxValue;
      float minZ=int.MaxValue;
      float maxX=int.MinValue;
      float maxY=int.MinValue;
      float maxZ=int.MinValue;
      for (int i=0; i<p.points.Length; i++){
        Vector3 use=p.points[i];
        float valX=use.x;
        float valY=use.y;
        float valZ=use.z;
        if (valX<minX) minX=valX;
        if (valY<minY) minY=valY;
        if (valZ<minZ) minZ=valZ;
        if (valX>maxX) maxX=valX;
        if (valY>maxY) maxY=valY;
        if (valZ>maxZ) maxZ=valZ;
      }
      return new float[]{minX,minY-p.height / 2 * Math.Abs(p.transform.localScale.y),minZ,maxX,maxY+p.height / 2 * Math.Abs(p.transform.localScale.y), maxZ};
    }
    public void merge(List<float> p, int l, int m, int r){
        int n1 = m - l + 1;
        int n2 = r - m;
        float[] L = new float[n1];
        float[] R = new float[n2];
        int i, j;
        for (i = 0; i < n1; ++i)
            L[i] =  p[l + i];
        for (j = 0; j < n2; ++j)
            R[j] = p[m + 1 + j];
        i = 0;
        j = 0;
        int k = l;
        while (i < n1 && j < n2) {
            if (L[i]<= R[j]) {
                p[k] = L[i];
                i++;
            }  else {
                p[k] = R[j];
                j++;
            }
            k++;
        }
        while (i < n1) {
            p[k] = L[i];
            i++;
            k++;
        }
        while (j < n2) {
            p[k] = R[j];
            j++;
            k++;
        }
    }

    public void sort(List<float> p, int l, int r){
        if (l < r) {
            int m = l+ (r-l)/2;
            sort(p, l, m);
            sort(p, m + 1, r);
            merge(p, l, m, r);
        }
    }

    private bool CheckCollision(PrismCollision collision){
        bool isCollision = false;
        Vector3 penetration_depth_vector = Vector3.zero;
        Prism prismA = collision.a;
        Prism prismB = collision.b;
        Vector3[] MKDiffPoints = MKDiff(prismA, prismB);
        List<Vector3> Simplex = new List<Vector3>();
        Vector3 w = MKDiffPoints[0];
        w = getSupportingPoint(MKDiffPoints, Vector3.right);
        Simplex.Add(w);
        Vector3 v = -w;
        while (true){
          w=getSupportingPoint(MKDiffPoints, v);
          if (Dot(w,v)<=0) break;
          Simplex.Insert(0,w);
          if (maxPrismScaleY==0){
            if (NextSimplex2D(ref Simplex, ref v)) {
  			       isCollision=true;
               break;
  		      }
          } else {
            if (NextSimplex(ref Simplex, ref v)) {
  			       isCollision=true;
               break;
  		      }
          }
        }
        if (isCollision == false){
            penetration_depth_vector = Vector3.zero;
        } else {
            penetration_depth_vector=EPA(Simplex,MKDiffPoints);
        }

        collision.penetrationDepthVectorAB=penetration_depth_vector;
        return isCollision ;
    }

    Vector3 EPA(List<Vector3> expandingPolygon, Vector3[] MKDiffPoints) {
      float tolerance = (float) Math.Pow(10,-5); //10 to the power of -5
      Vector3 depth_vector = Vector3.zero;
      while (true){
          Vector3 new_depth_vector = FindClosestPointFromOrigin(expandingPolygon.ToArray());
          if (Vector3.Distance(depth_vector, new_depth_vector) < tolerance){
              return new_depth_vector*(1.0001f);
          }
          depth_vector = new_depth_vector;
          Vector3 w = getSupportingPoint(MKDiffPoints, depth_vector);
          expandingPolygon.Add(w);
        }
    }

    bool NextSimplex( ref List<Vector3> points,	ref Vector3 direction){
      switch (points.Count()) {
      	case 2: return Line       (ref points, ref direction);
      	case 3: return Triangle   (ref points, ref direction);
      	case 4: return Tetrahedron(ref points, ref direction);
      }
      return false;
    }
    bool SameDirection(Vector3 direction, Vector3 ao){
    	return Dot(direction, ao) > 0;
    }
    bool Line(ref List<Vector3> points,	ref Vector3 direction){
    	Vector3 a = points[0];
    	Vector3 b = points[1];
    	Vector3 ab = b - a;
    	Vector3 ao =   - a;
    	if (SameDirection(ab, ao)) {
      	direction = cross(cross(ab,ao),ab);
      } else {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
      	points = temp;
      	direction = ao;
      }
      return false;
    }
    bool Triangle(ref List<Vector3> points,	ref Vector3 direction){
      Vector3 a = points[0];
    	Vector3 b = points[1];
    	Vector3 c = points[2];

    	Vector3 ab = b - a;
    	Vector3 ac = c - a;
    	Vector3 ao =   - a;

    	Vector3 abc = cross(ab,ac);

    	if (SameDirection(cross(abc,ac),ao)) {
    		if (SameDirection(ac, ao)) {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(c);
      		points = temp;
    			direction = cross(cross(ac,ao),ac);
    		}

    		else {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(b);
      		points = temp;
    			return Line(ref points , ref direction);
    		}
    	}

    	else {
    		if (SameDirection(cross(ab,abc), ao)) {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(b);
      		points = temp;
    			return Line(ref points, ref direction);
    		}

    		else {
    			if (SameDirection(abc, ao)) {
    				direction = abc;
    			}
    			else {
            List<Vector3> temp= new List<Vector3>();
            temp.Add(a);
            temp.Add(c);
            temp.Add(b);
            points = temp;
    				direction = -abc;
    			}
    		}
    	}

    	return false;
    }
    bool Tetrahedron (ref List<Vector3> points,	ref Vector3 direction)
    {

    	Vector3 a = points[0];
    	Vector3 b = points[1];
    	Vector3 c = points[2];
    	Vector3 d = points[3];

    	Vector3 ab = b - a;
    	Vector3 ac = c - a;
    	Vector3 ad = d - a;
    	Vector3 ao =   - a;

    	Vector3 abc = cross(ab,ac);
    	Vector3 acd = cross(ac,ad);
    	Vector3 adb = cross(ad,ab);

    	if (SameDirection(abc, ao)) {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
        temp.Add(b);
        temp.Add(c);
        points = temp;
    		return Triangle(ref points, ref direction);
    	}

    	if (SameDirection(acd, ao)) {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
        temp.Add(c);
        temp.Add(d);
        points = temp;
    		return Triangle(ref points , ref direction);
    	}

    	if (SameDirection(adb, ao)) {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
        temp.Add(d);
        temp.Add(b);
    		return Triangle(ref points, ref direction);
    	}

    	return true;
    }
    bool NextSimplex2D( ref List<Vector3> points,	ref Vector3 direction){
    	switch (points.Count()) {
    		case 2: return Line2D       (ref points, ref direction);
    		case 3: return Triangle2D   (ref points, ref direction);
    	}
    	return false;
    }
    bool Line2D(ref List<Vector3> points,	ref Vector3 direction){
    	Vector3 a = points[0];
    	Vector3 b = points[1];
    	Vector3 ab = b - a;
    	Vector3 ao =   - a;
    	if (SameDirection(ab, ao)) {
        direction = cross(cross(ab,ao),ab);
    	}	else {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
    		points = temp;
    		direction = ao;
    	}
    	return false;
    }
    bool Triangle2D(ref List<Vector3> points,	ref Vector3 direction){
      Vector3 a = points[0];
    	Vector3 b = points[1];
    	Vector3 c = points[2];

    	Vector3 ab = b - a;
    	Vector3 ac = c - a;
    	Vector3 ao =   - a;

    	Vector3 abc = cross(ab,ac);

    	if (SameDirection(cross(abc,ac),ao)) {
    		if (SameDirection(ac, ao)) {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(c);
      		points = temp;
    			direction = cross(cross(ac,ao),ac);
    		}

    		else {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(b);
      		points = temp;
    			return Line2D(ref points , ref direction);
    		}
    	}
    	else {
        if (SameDirection(cross(ab,abc),ao)) {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(b);
      		points = temp;
    			return Line2D(ref points, ref direction);
    		}
    		else {
    			return true;
    		}
    	}
    	return false;
    }

    private Vector3[] MKDiff(Prism prismA, Prism prismB)
    {
      if (maxPrismScaleY==0){
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
      } else {
        Vector3[] result = new Vector3[4*prismA.points.Length * prismB.points.Length];
        int k = 0;
        for (int i = 0; i < prismA.points.Length; i++)
        {
            for (int j = 0; j < prismB.points.Length; j++)
            {
                Vector3 point = new Vector3(prismA.points[i].x - prismB.points[j].x, prismA.points[i].y+prismA.height / 2 * Math.Abs(prismA.transform.localScale.y) - prismB.points[j].y+prismB.height / 2 * Math.Abs(prismB.transform.localScale.y), prismA.points[i].z - prismB.points[j].z);
                result[k] = point;
                k++;
                point = new Vector3(prismA.points[i].x - prismB.points[j].x, prismA.points[i].y-prismA.height / 2 * Math.Abs(prismA.transform.localScale.y) - prismB.points[j].y+prismB.height / 2 * Math.Abs(prismB.transform.localScale.y), prismA.points[i].z - prismB.points[j].z);
                result[k] = point;
                k++;
                point = new Vector3(prismA.points[i].x - prismB.points[j].x, prismA.points[i].y+prismA.height / 2 * Math.Abs(prismA.transform.localScale.y) - prismB.points[j].y-prismB.height / 2 * Math.Abs(prismB.transform.localScale.y), prismA.points[i].z - prismB.points[j].z);
                result[k] = point;
                k++;
                point = new Vector3(prismA.points[i].x - prismB.points[j].x, prismA.points[i].y-prismA.height / 2 * Math.Abs(prismA.transform.localScale.y) - prismB.points[j].y-prismB.height / 2 * Math.Abs(prismB.transform.localScale.y), prismA.points[i].z - prismB.points[j].z);
                result[k] = point;
                k++;
            }
        }
        return result;
      }

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
    private static Vector3 cross(Vector3 vec, Vector3 other)
    {
        return new Vector3(vec.y*other.z-vec.z*other.y, vec.z*other.x-vec.x*other.z, vec.x*other.y-vec.y*other.x);
    }



    private Vector3 getSupportingPoint(Vector3[] MKDiffPoints, Vector3 v)
    {
        float max=float.MinValue;
        Vector3 support=Vector3.zero;
        for (int i=0; i<MKDiffPoints.Length; i++){
          Vector3 d=MKDiffPoints[i];
          float dot=Dot(v,d);
          if (dot>max){
            //Debug.Log("NEW MAX IS "+ d+" "+v);
            max=dot;
            support=d;
          }
        }
        return support;
    }

    private Vector3 FindClosestPointFromOrigin(Vector3[] Simplex)
    {
        float min = float.MaxValue;
        float distance = 0;
        Vector3 result = new Vector3(0, 0, 0);
        for (int i = 0; i < Simplex.Length; i++)
        {
            distance = distanceFromOrigin(Simplex[i]);
            if (distance < min)
            {
                result = Simplex[i];
                min=distance;
            }
        }
        return result;
    }
    private Vector3 FindFarthestPointFromOrigin(Vector3[] Simplex)
    {
        float max = float.MinValue;
        float distance = 0;
        Vector3 result = new Vector3(0, 0, 0);
        for (int i = 0; i < Simplex.Length; i++)
        {
            distance = distanceFromOrigin(Simplex[i]);
            if (distance > max)
            {
                result = Simplex[i];
                max=distance;
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
        prismObjA.transform.position += pushA;
        prismObjB.transform.position += pushB;

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
    private class KDTree
    {
        public Vector3 location;
        public KDTree leftChild;
        public KDTree rightChild;

        public KDTree(List<Vector3> points, int depth){
            List <Vector3> temp = points;
            int axis = depth%3;
            if(points.Count == 0){
                return;
            }
            sortVector(temp, 0, temp.Count-1, axis);
            int median = temp.Count/2;
            this.location = temp[median];
            List <Vector3> left = new List <Vector3> ();
            List <Vector3> right = new List <Vector3> ();
            splitList(temp, left, right);
            if(left.Count > 0){
                this.leftChild = new KDTree(left, depth+1);
            }
            if(right.Count > 0){
                this.rightChild = new KDTree(right, depth+1);
            }
        }

        public void splitList(List<Vector3> points, List<Vector3> left, List<Vector3> right){
            int median = points.Count/2;
            for(int i = 0; i < median; i++){
                left.Add(points[i]);
            }
            for(int i = median+1; i < points.Count; i++){
                right.Add(points[i]);
            }
        }

        public void mergeVector(List<Vector3> p, int l, int m, int r, int dim){
            int n1 = m - l + 1;
            int n2 = r - m;
            Vector3[] L = new Vector3[n1];
            Vector3[] R = new Vector3[n2];
            int i, j;
            for (i = 0; i < n1; ++i)
                L[i] =  p[l + i];
            for (j = 0; j < n2; ++j)
                R[j] = p[m + 1 + j];

            i = 0;
            j = 0;

            int k = l;
            while (i < n1 && j < n2) {
                if(dim == 0){
                    if (L[i].x<= R[j].x) {
                        p[k] = L[i];
                        i++;
                    }
                    else {
                        p[k] = R[j];
                        j++;
                    }
                    k++;
                }
                else if (dim == 1){
                    if (L[i].y<= R[j].y) {
                        p[k] = L[i];
                        i++;
                    }
                    else {
                        p[k] = R[j];
                        j++;
                    }
                    k++;
                }
                else if (dim == 2){
                    if (L[i].z<= R[j].z) {
                        p[k] = L[i];
                        i++;
                    }
                    else {
                        p[k] = R[j];
                        j++;
                    }
                    k++;
                }
            }

            while (i < n1) {
                p[k] = L[i];
                i++;
                k++;
            }

            while (j < n2) {
                p[k] = R[j];
                j++;
                k++;
            }
        }

        public void sortVector(List<Vector3> p, int l, int r, int dim){
            if (l < r) {
                int m = l+ (r-l)/2;
                sortVector(p, l, m, dim);
                sortVector(p, m + 1, r, dim);
                mergeVector(p, l, m, r, dim);
            }
        }
    }
    #endregion
}
