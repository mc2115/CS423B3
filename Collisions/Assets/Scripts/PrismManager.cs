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

    private IEnumerable<PrismCollision> PotentialCollisions1()
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
        var collisionsY= new List<PrismCollision>();
        List<Prism> activeListY = new List<Prism>();
        for (int i=0; i<ycoord.Count; i++){
          Prism p=dictY[ycoord[i]];
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
        if (collisionsY.Count==0){
          for (int i=0; i<collisionsX.Count; i++){
            PrismCollision colX=collisionsX[i];
            for (int j=0; j<collisionsZ.Count; j++){
              PrismCollision colZ=collisionsZ[j];
              if (collEquals(colX, colZ)){
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
                if (collEquals(colX, colY)&& collEquals (colY, colZ)){
                  yield return colX;
                }
              }
            }
          }
        }
        yield break;
    }
    private IEnumerable<PrismCollision> PotentialCollisions()
   {
       Dictionary <Vector3, Prism> dict = new Dictionary <Vector3, Prism> ();
       List <Vector3> points = new List <Vector3>();
       /*for (int i = 0; i < prisms.Count; i++) {
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
       }*/
       for (int i = 0; i < prisms.Count; i++) {
            float[] temp=minMaxXY(prisms[i]);
            Vector3 min = new Vector3(temp[0], temp[2], temp[4]);
            Vector3 max = new Vector3(temp[1], temp[3], temp[5]);
            Prism val=prisms[i];
            dict[min] = val;
            dict[max] = val;
            points.Add(min);
            points.Add(max);
            for(int j = 0; j < prisms[i].points.Length; j++){
                points.Add(prisms[i].points[j]);
                dict[prisms[i].points[j]] = prisms[i];
            }
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
           //Debug.Log("xD");
           return;
       }

       if(root.leftChild != null){
           traverseTree(root.leftChild, activeList, dict, collisions);
       }
       //Debug.Log("root: " + root.location);
       Prism p = dict[root.location];
       if(activeList.Contains(p)){
           int index = activeList.IndexOf(p);
           Color c=UnityEngine.Random.ColorHSV();
           Color d=UnityEngine.Random.ColorHSV();
           for (int j = index+1; j < activeList.Count; j++){
               //DrawBBox(p,c);
               //DrawBBox(activeList[j],c);
               PrismCollision coll = new PrismCollision();
               coll.a=p;
               coll.b=activeList[j];
               //Debug.Log("collision");
               collisions.Add(coll);
           }
       }
       else{
           activeList.Add(p);
           //Debug.Log("added prism");
       }
       if(root.rightChild != null){
           traverseTree(root.rightChild, activeList, dict, collisions);
       }
   }
   private void DrawBBox(Prism a, Color c){
     float[] temp=minMaxXY(a);
     Debug.DrawLine(new Vector3(temp[0], 0, temp[2]), new Vector3(temp[0],0,temp[5]),c);
     Debug.DrawLine(new Vector3(temp[0], 0, temp[5]), new Vector3(temp[3],0,temp[5]),c);
     Debug.DrawLine(new Vector3(temp[3], 0, temp[5]), new Vector3(temp[3],0,temp[2]),c);
     Debug.DrawLine(new Vector3(temp[3], 0, temp[2]), new Vector3(temp[0],0,temp[2]),c);
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
      //Debug.Log("prism "+p.prismObject+" height "+p.height / 2 * p.transform.localScale.y);
      //return new float[]{minX-(UnityEngine.Random.value * 0.00001f),minY-p.height / 2 * Math.Abs(p.transform.localScale.y),minZ-(UnityEngine.Random.value * 0.00001f),maxX+(UnityEngine.Random.value * 0.00001f),maxY+p.height / 2 * Math.Abs(p.transform.localScale.y), maxZ+(UnityEngine.Random.value * 0.00001f)};
      return new float[]{minX,minY-p.height / 2 * Math.Abs(p.transform.localScale.y),minZ,maxX,maxY+p.height / 2 * Math.Abs(p.transform.localScale.y), maxZ};
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
    //bool originally
    private bool CheckCollision1(PrismCollision collision)
    {

        var prismA = collision.a;
        var prismB = collision.b;
        //Debug.Log("We made it with "+prismA.prismObject.name+" "+prismB.prismObject.name);

        collision.penetrationDepthVectorAB = Vector3.zero;

        return true;
    }
    private bool CheckCollision(PrismCollision collision)
    {

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
            //Debug.Log("HAHAHA");
            penetration_depth_vector = Vector3.zero;
        } else {
            penetration_depth_vector=EPA(Simplex,MKDiffPoints);
            //Debug.Log("VECTOR IS "+penetration_depth_vector+" BETWEEN "+collision.a.prismObject.name+" AND "+collision.b.prismObject.name);
        }

        collision.penetrationDepthVectorAB=penetration_depth_vector;
        return isCollision ;
    }
    /*Vector3 EPA2D1(List<Vector3> polytope, Vector3[] diff) {
    	int minIndex = 0;
    	float minDistance = float.MaxValue;
    	Vector3 minNormal=Vector3.zero;

    	while (minDistance == float.MaxValue) {
    		for (int i = 0; i < polytope.Count; i++) {
    			int j = (i+1) % polytope.Count;

    			Vector3 vertexI = polytope[i];
    			Vector3 vertexJ = polytope[j];

    			Vector3 ij = vertexJ-vertexI;

    			Vector3 normal = new Vector3(ij.z, 0 ,-ij.x);
    			float distance = Dot(normal, vertexI);

    			if (distance < 0) {
    				distance *= -1;
    				normal*=-1;
    			}
    			if (distance < minDistance) {
    				minDistance = distance;
    				minNormal = normal;
    				minIndex = j;
            //Debug.Log("minDistance "+minDistance+" minNormal "+minNormal+" minIndex "+minIndex);
    			}
    		}
        Vector3 support = getSupportingPoint(diff, minNormal);
    		float sDistance = Dot(minNormal, support);

    		if(Math.Abs(sDistance - minDistance) > 0.00001) {
    		 	minDistance = float.MaxValue;
    			polytope.Insert(minIndex,support);
    		}
    	}
    	return minNormal;
    }*/
  Vector3 EPA(List<Vector3> expandingPolygon, Vector3[] MKDiffPoints) {
    float tolerance = (float) Math.Pow(10,-5); //10 to the power of -5
    Vector3 depth_vector = Vector3.zero;
    while (true){
        Vector3 new_depth_vector = FindClosestPointFromOrigin(expandingPolygon.ToArray());
        if (Vector3.Distance(depth_vector, new_depth_vector) < tolerance){
            return new_depth_vector*(1+UnityEngine.Random.value * 0.0001f);
            //return new_depth_vector;
        }
        depth_vector = new_depth_vector;
        Vector3 w = getSupportingPoint(MKDiffPoints, depth_vector);
        expandingPolygon.Add(w);
      }
  }
  /*  Vector3 EPA3D(List<Vector3> simplex, Vector3[] diff){
    	List<Vector3> polytope=simplex;
    	List<float>  faces = new List<float>() {0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2};
      ArrayList temp=GetFaceNormals(polytope, faces);
      List<Vector3> normals_vec=(List<Vector3>) temp[0];
      List<float> normals_dis=(List<float>) temp[1];
      int minFace=(int) temp[2];
      Vector3 minNormal=normals_vec[minFace];
    	float   minDistance = float.MaxValue;
    	while (minDistance == float.MaxValue) {
    		minNormal  = normals_vec[minFace];
    		minDistance = normals_dis[minFace];
        //Debug.Log("calculating minNormal is " + minNormal+" minDistance " + minDistance);
    		Vector3 support = getSupportingPoint(diff, minNormal);
    		float sDistance = Dot(minNormal,support);
    		if (Math.Abs(sDistance - minDistance) > 0.00001f) {
    			minDistance = float.MaxValue;
          List<Vector2> uniqueEdges=new List<Vector2>();
      			for (int i = 0; i < normals_vec.Count; i++) {
      				if (SameDirection(normals_vec[i], support)) {
      					int f = i * 3;
      					AddIfUniqueEdge(uniqueEdges, faces, f,     f + 1);
      					AddIfUniqueEdge(uniqueEdges, faces, f + 1, f + 2);
      					AddIfUniqueEdge(uniqueEdges, faces, f + 2, f    );
      					faces[f + 2] = faces.Last(); faces.RemoveAt(faces.Count-1);
      					faces[f + 1] = faces.Last(); faces.RemoveAt(faces.Count-1);
      					faces[f    ] = faces.Last(); faces.RemoveAt(faces.Count-1);

                normals_vec[i]=normals_vec.Last(); normals_vec.RemoveAt(normals_vec.Count-1);
                normals_dis[i]=normals_dis.Last(); normals_dis.RemoveAt(normals_dis.Count-1);
      					i--;
      				}
      			}
            List<float> newFaces=new List<float>();
      			for (int j=0; j<uniqueEdges.Count; j++) {
              Vector2 tempV=uniqueEdges[j];
              float edgeIndex1=tempV.x;
              float edgeIndex2=tempV.y;
      				newFaces.Add(edgeIndex1);
      				newFaces.Add(edgeIndex2);
      				newFaces.Add(polytope.Count);
      			}
  			    polytope.Add(support);
            ArrayList tempN=GetFaceNormals(polytope, newFaces);
            List<Vector3> normals_vecN=(List<Vector3>) tempN[0];
            List<float> normals_disN=(List<float>) tempN[1];
            int newMinFace=(int) tempN[2];
            float oldMinDistance = float.MaxValue;
      			for (int i = 0; i < normals_vecN.Count; i++) {
      				if (normals_disN[i] < oldMinDistance) {
      					oldMinDistance = normals_disN[i];
      					minFace = i;
      				}
      			}
      			if (normals_disN[newMinFace] < oldMinDistance) {
      				minFace = newMinFace + normals_dis.Count;
      			}
            for (int j=0; j<newFaces.Count; j++){
              faces.Add(newFaces[j]);
            }
            for (int j=0; j<normals_vecN.Count; j++){
              normals_vec.Add(normals_vecN[j]);
            }
            for (int j=0; j<normals_disN.Count; j++){
              normals_dis.Add(normals_disN[j]);
            }
      		}
      	}
      //Debug.Log("minNormal is " + minNormal);
      return minNormal*(minDistance);
      //return getSupportingPoint(diff, minNormal);
    }
    ArrayList GetFaceNormals(List<Vector3> polytope, List<float>  faces)
    {
    	List<Vector3> normal_vec=new List<Vector3>();
      List<float> normal_dis=new List<float>();
    	int minTriangle = 0;
    	float  minDistance = float.MaxValue;

    	for (int i = 0; i < faces.Count; i += 3) {
    		Vector3 a = polytope[(int) faces[i    ]];
    		Vector3 b = polytope[(int) faces[i + 1]];
    		Vector3 c = polytope[(int) faces[i + 2]];
    		Vector3 normal = cross((b - a),(c - a)).normalized;
        //Vector3 normal = FindClosestPointFromOrigin(new Vector3[] {(a),(b),(c)});
    		float distance = Dot(normal, a);

    		if (distance < 0) {
    			normal   *= -1;
    			distance *= -1;
    		}
    		normal_vec.Add(normal);
        normal_dis.Add(distance);

    		if (distance < minDistance) {
    			minTriangle = i / 3;
    			minDistance = distance;
    		}
    	}
      ArrayList temp=new ArrayList();
      temp.Add(normal_vec);
      temp.Add(normal_dis);
      temp.Add(minTriangle);
    	return temp;
    }
    void AddIfUniqueEdge(List<Vector2> edges, List<float> faces, int a, int b)
    {
      Vector2 search=new Vector2(faces[b], faces[a]);
      int look=edges.IndexOf(search);
      if (look!=-1){
        Vector2 reverse = edges[look];
      	if (reverse != edges[edges.Count-1]) {
      		edges.RemoveAt(look);
      	} else {
      		edges.Add(new Vector2(faces[a], faces[b]));
      	}
      } else {
        edges.Add(new Vector2(faces[a], faces[b]));
      }
    }*/
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
    	}
    	else {
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

    		//direction = ao;
        //Debug.Log("AO IS "+ao+" AB IS "+ab+" BROKEN CROSS PRODUCT IS + "+ cross(cross(ab,ao),ab));
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
    bool Triangle2D(ref List<Vector3> points,	ref Vector3 direction){
      Vector3 a = points[0];
    	Vector3 b = points[1];
    	Vector3 c = points[2];

    	Vector3 ab = b - a;
    	Vector3 ac = c - a;
    	Vector3 ao =   - a;

    	Vector3 abc = cross(ab,ac);

    	if (SameDirection(cross(abc,ac),ao)) {
      //if (SameDirection(new Vector3(ac.z , 0 , -ac.x),ao)) {
    		if (SameDirection(ac, ao)) {
          List<Vector3> temp= new List<Vector3>();
          temp.Add(a);
          temp.Add(c);
      		points = temp;
    			direction = cross(cross(ac,ao),ac);
          //direction=new Vector3(-ac.z, 0 , ac.x);
          //direction=ac;
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
    		//if (SameDirection(new Vector3(-ab.z, 0, ab.x), ao)) {
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
        //Debug.Log(collision.penetrationDepthVectorAB);
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
            //Debug.Log("Location " + temp[median]);
            for(int i = 0; i < temp.Count; i++){
                //Debug.Log("Adding " + temp[i]);
            }
            List <Vector3> left = new List <Vector3> ();
            List <Vector3> right = new List <Vector3> ();
            splitList(temp, left, right);
            for(int i = 0; i < left.Count; i++){
                //Debug.Log("Adding Left" + left[i]);
            }
            for(int i = 0; i < right.Count; i++){
                //Debug.Log("Adding Right" + right[i]);
            }
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
            // Create temp arrays
            Vector3[] L = new Vector3[n1];
            Vector3[] R = new Vector3[n2];
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
                // Find the middle
                // point
                int m = l+ (r-l)/2;

                // Sort first and
                // second halves
                sortVector(p, l, m, dim);
                sortVector(p, m + 1, r, dim);

                // Merge the sorted halves
                mergeVector(p, l, m, r, dim);
            }
        }
    }
    #endregion
}
