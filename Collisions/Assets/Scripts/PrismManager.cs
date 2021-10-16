using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private Dictionary<Prism,bool> prismColliding = new Dictionary<Prism, bool>();

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

    private IEnumerable<PrismCollision> PotentialCollisions2()
    {
        for (int i = 0; i < prisms.Count; i++) {
            for (int j = i + 1; j < prisms.Count; j++) {
                var checkPrisms = new PrismCollision();
                checkPrisms.a = prisms[i];
                checkPrisms.b = prisms[j];

                yield return checkPrisms;
            }
        }

        yield break;
    }
    /*private IEnumerable<PrismCollision> PotentialCollisions1()
    {
        Dictionary<float, ArrayList> dict=new Dictionary<float, ArrayList>();
        ArrayList xcoord=new ArrayList();
        ArrayList ycoord=new ArrayList();
        for (int i = 0; i < prisms.Count; i++) {
            float[] temp=minMaxXY(prisms[i]);
            ArrayList val=new ArrayList();
            val.Add(prisms[i]);
            val.Add(temp);
            dict[temp[0]]=val;
            dict[temp[1]]=val;
            dict[temp[2]]=val;
            dict[temp[3]]=val;
            xcoord.add(temp[0]);
            xcoord.add(temp[2]);
            ycoord.add(temp[1]);
            ycoord.add(temp[3]);
        }
        sort(xcoord,0,xcoord.Count-1);
        sort(ycoord,0,ycoord.Count-1);
        var activeList = new ArrayList();

        int c = 0; //c is the index of the prism that we are checking collisions for
        Prism cPris=(Prism)p[c];
        float[] temp=minMaxXY(cPris);
        float cminX=temp[0];
        float cminY=temp[1];
        float cmaxX=temp[2];
        float cmaxY=temp[3];
        for(int i = 0; i < p.Count; i++){
            Prism pris=(Prism) p[i];
            float[] vals=minMaxXY(pris);
            float minX=vals[0];
            float minY=vals[1];
            float maxX=vals[2];
            float maxY=vals[3];
            if(i == 0){  // base case, put the first item in the active list
                activeList.Add(pris);
            }
            else if(minX <= cmaxX){  // if selected prism has potential collision, add to active list
                //Debug.Log(minX+" less than "+cmaxX);
                activeList.Add(p[i]);
                var collision=new PrismCollision();
                collision.a=(Prism) p[i];
                collision.b=(Prism) p[c];
                yield return collision;
            }
            else if(minX> cmaxX){ // if selected prism is not a potential collision, check for collisions for all prisms in active list
                //checkCollisions(activeList);
                Debug.Log(minX+" greater than "+cmaxX);
                activeList.Add(p[i]);
                for(int j = c ; j < i; j++){    // iterate and delete all items no longer a potential collision with selected prism
                    if(minX> minMaxXY((Prism) p[j])[2]){
                        activeList.Remove((Prism) p[j]);
                    }
                    c = j;
                    temp=minMaxXY((Prism) p[c]);
                    cminX=temp[0];
                    cminY=temp[1];
                    cmaxX=temp[2];
                    cmaxY=temp[3];
                }
            }

        }
        yield break;
    }*/
    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        Dictionary<float, Prism> dictX=new Dictionary<float, Prism>();
        Dictionary<float, Prism> dictY=new Dictionary<float, Prism>();
        List<float> xcoord=new List<float>();
        List<float> ycoord=new List<float>();
        for (int i = 0; i < prisms.Count; i++) {
            float[] temp=minMaxXY(prisms[i]);
            Prism val=prisms[i];
            dictX[temp[0]]=val;
            dictY[temp[1]]=val;
            dictX[temp[2]]=val;
            dictY[temp[3]]=val;
            xcoord.Add(temp[0]);
            xcoord.Add(temp[2]);
            ycoord.Add(temp[1]);
            ycoord.Add(temp[3]);
        }

        sort(xcoord,0,xcoord.Count-1);
        sort(ycoord,0,ycoord.Count-1);
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

        for (int i=0; i<collisionsX.Count; i++){
          PrismCollision colX=collisionsX[i];
          for (int j=0; j<collisionsY.Count; j++){
            PrismCollision colY=collisionsY[j];
            if ((colX.a==colY.a && colX.b==colY.b)||(colX.a==colY.b && colX.b==colY.a)){
              yield return colX;
            }
          }
        }
        yield break;
    }

    private static float[] minMaxXY(Prism p){
      float minX=int.MaxValue;
      float minY=int.MaxValue;
      float maxX=int.MinValue;
      float maxY=int.MinValue;
      for (int i=0; i<p.points.Length; i++){
        Vector3 use=p.points[i];
        //Debug.Log(p.name+" "+use);
        float valX=use.x;
        float valY=use.z;
        if (valX<minX) minX=valX;
        if (valY<minY) minY=valY;
        if (valX>maxX) maxX=valX;
        if (valY>maxY) maxY=valY;
      }
      //Debug.Log("Name "+p.name+" minX "+minX+" minY "+minY+" maxX "+ maxX+" maxY "+maxY);
      return new float[]{minX,minY,maxX,maxY};
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


    private bool CheckCollision(PrismCollision collision)
    {

        var prismA = collision.a;
        var prismB = collision.b;
        Debug.Log("We made it with "+prismA.prismObject.name+" "+prismB.prismObject.name);

        collision.penetrationDepthVectorAB = Vector3.zero;

        return true;
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

    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
