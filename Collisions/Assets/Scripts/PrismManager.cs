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
            //var randPointCount = Mathf.RoundToInt(3);
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

    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        Dictionary<float, Prism> dictX=new Dictionary<float, Prism>();
        Dictionary<float, Prism> dictY=new Dictionary<float, Prism>();
        Dictionary<float, Prism> dictZ=new Dictionary<float, Prism>();
        List<float> xcoord=new List<float>();
        List<float> ycoord=new List<float>();
        List<float> zcoord=new List<float>();
        for (int i = 0; i < prisms.Count; i++) {
            float[] temp=minMaxXY(prisms[i]);
            Prism val=prisms[i];
            dictX[temp[0]]=val;
            dictY[temp[1]]=val;
            dictZ[temp[2]]=val;
            dictX[temp[3]]=val;
            dictY[temp[4]]=val;
            dictZ[temp[5]]=val;
            xcoord.Add(temp[0]);
            xcoord.Add(temp[3]);
            ycoord.Add(temp[1]);
            ycoord.Add(temp[4]);
            zcoord.Add(temp[2]);
            zcoord.Add(temp[5]);
        }

        sort(xcoord,0,xcoord.Count-1);
        sort(ycoord,0,ycoord.Count-1);
        sort(zcoord,0,zcoord.Count-1);
        var collisionsX= new List<PrismCollision>();
        List<Prism> activeListX = new List<Prism>();
        for (int i=0; i<xcoord.Count; i++){
          Prism p= dictX[xcoord[i]];
          //float[] temp= (float[]) dict.get(xcoord[i])[1];
          if (activeListX.Contains(p)){
            int index=activeListX.IndexOf(p);
            if (index!=activeListX.Count-1){
              for (int j=index+1; j< activeListX.Count; j++){
                PrismCollision coll = new PrismCollision();
                coll.a=p;
                coll.b= activeListX[j];
                collisionsX.Add(coll);
              }
            }
          } else {
            activeListX.Add(p);
          }
        }
        //Debug.Log(ycoord);
        var collisionsY= new List<PrismCollision>();
        List<Prism> activeListY = new List<Prism>();
        for (int i=0; i<ycoord.Count; i++){

          Prism p=dictY[ycoord[i]];
          //float[] temp= (float[]) dict.get(ycoord[i])[1];
          if (activeListY.Contains(p)){
            int index=activeListY.IndexOf(p);
            if (index!=activeListY.Count-1){
              for (int j=index+1; j< activeListY.Count; j++){
                PrismCollision coll = new PrismCollision();
                coll.a=p;
                coll.b=activeListY[j];
                collisionsY.Add(coll);
              }
            }
          } else {
            activeListY.Add(p);
          }
        }
        var collisionsZ= new List<PrismCollision>();
        List<Prism> activeListZ = new List<Prism>();
        for (int i=0; i<zcoord.Count; i++){
          Prism p=dictZ[zcoord[i]];
          //float[] temp= (float[]) dict.get(ycoord[i])[1];
          if (activeListZ.Contains(p)){
            int index=activeListZ.IndexOf(p);
            if (index!=activeListZ.Count-1){
              for (int j=index+1; j< activeListZ.Count; j++){
                PrismCollision coll = new PrismCollision();
                coll.a=p;
                coll.b=activeListZ[j];
                collisionsZ.Add(coll);
              }
            }
          } else {
            activeListZ.Add(p);
          }
        }
        //Debug.Log(collisionsX.Count+" "+collisionsY.Count+" "+collisionsZ.Count);
        if (collisionsY.Count==0){
          for (int i=0; i<collisionsX.Count; i++){
            PrismCollision colX=collisionsX[i];
            for (int j=0; j<collisionsZ.Count; j++){
              PrismCollision colZ=collisionsZ[j];
              if (collEquals(colX, colZ)){
                Debug.Log("Collision!");
                yield return colX;
              }
            }
          }
        } else {
          for (int i=0; i<collisionsX.Count; i++){
            PrismCollision colX=collisionsX[i];
            for (int j=0; j<collisionsY.Count; j++){
              PrismCollision colY=collisionsY[j];
              for (int k=0; k<collisionsZ.Count; k++){
                PrismCollision colZ=collisionsZ[k];
                //Debug.Log(colX.a.name+" "+colX.b.name+" "+colY.a.name+" "+colY.b.name+" "+colZ.a.name+" "+colZ.b.name+" ");
                if (collEquals(colX, colY)&& collEquals (colY, colZ)){
                  //Debug.Log("Collision!");
                  yield return colX;
                }
              }
            }
          }
        }
        yield break;
    }
    private bool collEquals(PrismCollision colX, PrismCollision colY){
      //Debug.Log("Collision?");
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
        //Debug.Log(p.name+" "+use.y);
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
      //Debug.Log("Name "+p.name+" minX "+minX+" minY "+minY+" maxX "+ maxX+" maxY "+maxY+" minZ "+ minZ+" maxZ "+maxZ);
      return new float[]{minX,minY-p.height,minZ,maxX,maxY+p.height,maxZ};
    }
    public void merge(List<float> p, int l, int m, int r){
        int n1 = m - l + 1;
        int n2 = r - m;
        // Create temp arrays
        float[] L = new float[n1];
        float[] R = new float[n2];
        int i, j;

        // Copy data to temp arrays
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
            }
            else {
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
            // Find the middle
            // point
            int m = l+ (r-l)/2;

            // Sort first and
            // second halves
            sort(p, l, m);
            sort(p, m + 1, r);

            // Merge the sorted halves
            merge(p, l, m, r);
        }
    }

    private Node CheckCollision(PrismCollision collision)
    {
        float tolerance = 0f; //SET LATER
        bool isCollision = false;
        Vector3 penetration_depth_vector = Vector3.zero;
        Prism prismA = collision.a;
        Prism prismB = collision.b;

        // collision.penetrationDepthVectorAB = Vector3.zero;

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

            v = new_v;
            w = getSupportingPoint(MKDiffPoints, v);
            Simplex.Add(w);

            isCollision = DoesSimplexContainOrigin(Simplex);


            if (!isCollision)
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
                    expandingPolygon.AddBetweenClosestEdge(w);
                }
            }
        }
        Node ans = new Node(isCollision, penetration_depth_vector);
        return ans ;
    }

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
            int j = i - 1;

            while (j >= 0 && distance[j] > key)
            {
                distance[j + 1] = distance[j];
                j = j - 1;
            }
            arr[j + 1] = key;
        }

        return distance[0];
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
    private bool CheckCollision(PrismCollision collision)
    {
        float tolerance = (float) Math.Pow(10,-5); //10 to the power of -5
        bool isCollision = false;
        Vector3 penetration_depth_vector = Vector3.zero;
        Prism prismA = collision.a;
        Prism prismB = collision.b;
        Vector3[] MKDiffPoints = MKDiff(prismA, prismB);
        List<Vector3> Simplex = new List<Vector3>();

        Vector3 w = MKDiffPoints[0];

        w = getSupportingPoint(MKDiffPoints, w);
        Simplex.Add(w);
        Vector3 v = -w;
        while (true){
          w=getSupportingPoint(MKDiffPoints, v);
          if (Dot(w,v)<=0) break;
          Simplex.Add(w);
          if (NextSimplex(Simplex,v)) {
			       isCollision=true;
             break;
		      }
        }

        if (isCollision == false)
        {
            Debug.Log("PRANKED XD");
            penetration_depth_vector = Vector3.zero;
        }
        else
        {
            List<Vector3> expandingPolygon = Simplex;
            Vector3 depth_vector = Vector3.zero;

            while (true)
            {
                Vector3 new_depth_vector = FindClosestPointFromOrigin(expandingPolygon.ToArray());
                Debug.Log("new vector "+new_depth_vector );
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

        collision.penetrationDepthVectorAB=penetration_depth_vector;
        Debug.Log(collision.penetrationDepthVectorAB);
        return isCollision ;
    }

    bool NextSimplex( List<Vector3> points,	Vector3 direction){
    	switch (points.Count()) {
    		case 2: return Line       (points, direction);
    		case 3: return Triangle   (points, direction);
    		case 4: return Tetrahedron(points, direction);
    	}

    	// never should be here
    	return false;
    }
    bool SameDirection(Vector3 direction, Vector3 ao){
    	return Dot(direction, ao) > 0;
    }
    bool Line(List<Vector3> points,	Vector3 direction){
    	Vector3 a = points[0];
    	Vector3 b = points[1];

    	Vector3 ab = b - a;
    	Vector3 ao =   - a;

    	if (SameDirection(ab, ao)) {
    		direction = cross(cross(ab,ao),ab);
    	}
    	else {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
    		points = temp;
    		direction = ao;
    	}
    	return false;
    }
    bool Triangle(List<Vector3> points,	Vector3 direction){
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
    			return Line(points , direction);
    		}
    	}

    	else {
    		if (SameDirection(cross(ab,abc), ao)) {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(b);
      		points = temp;
    			return Line(points, direction);
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
    bool Tetrahedron (List<Vector3> points,	Vector3 direction)
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
    		return Triangle(points, direction);
    	}

    	if (SameDirection(acd, ao)) {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
        temp.Add(c);
        temp.Add(d);
        points = temp;
    		return Triangle(points , direction);
    	}

    	if (SameDirection(adb, ao)) {
        List<Vector3> temp= new List<Vector3>();
        temp.Add(a);
        temp.Add(d);
        temp.Add(b);
    		return Triangle(points, direction);
    	}

    	return true;
    }

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
                //Debug.Log("kth point "+k+ " point is "+point);
                k++;
            }
        }
        /*for (int i=0; i<result.Length; i++){
          Debug.DrawLine(result[i], result[(i + 1) % result.Length] , Color.yellow);
        }*/
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
    private static Vector3 cross(Vector3 vec, Vector3 other)
    {
        return new Vector3(vec.y*other.z-vec.z*other.y, vec.z*other.x-vec.x*other.z, vec.x*other.y-vec.y*other.x);
    }


    private Vector3 getSupportingPoint2(Vector3[] MKDiffPoints, Vector3 v)
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
        Debug.Log(collision.penetrationDepthVectorAB);
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
