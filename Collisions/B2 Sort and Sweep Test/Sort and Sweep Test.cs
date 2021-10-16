using System;
using System.Collections;

namespace Sort_and_Sweep_Test{
    public class Sort_and_Sweep{
        public static void Main(String[] args){ //main method to test
            var prismList = new ArrayList(); //test arraylist
            prismList.Add(new Prism(0,0,1,1)); //just initializing the list w some values
            prismList.Add(new Prism(2, 1, 4, 10));
            prismList.Add(new Prism(1, 1, 5, 5));
            prismList.Add(new Prism(2, 1, 3, 9));
            prismList.Add(new Prism(6, 1, 10, 12));        
            prismList.Add(new Prism(7, 1, 9, 12));        
            prismList.Add(new Prism(7, 1, 12, 12));           
            prismList.Add(new Prism(11, 1, 16, 12));    
            prismList.Add(new Prism(12, 1, 20, 12));    
            //MERGESORT BABY
            MergeSort m = new MergeSort();              //SORT
            m.sort(prismList, 0, prismList.Count - 1);  //AND
            sweep(prismList);                           //SWEEP 
            
           /* for(int i = 0; i < prismList.Count; i++){
                Console.WriteLine(((Prism)prismList[i]).minX);
            } */                                                    //used to test mergesort

        }


        public static void sweep(ArrayList p){ //CURRENTLY ONLY IN ONE DIMENSION, BUT FROM WHAT I FOUND 2D IMPLEMENTATIONS JUST DO A 1D SWEEP OVER THE AXIS WITH THE AXIS W THE MOST VARIANCE
            var activeList = new ArrayList();

            int c = 0; //c is the index of the prism that we are checking collisions for
            for(int i = 0; i < p.Count; i++){ 
                if(i == 0){  // base case, put the first item in the active list
                    activeList.Add(p[i]);
                }
                else if(((Prism)p[i]).minX <= ((Prism)p[c]).maxX){  // if selected prism has potential collision, add to active list
                    activeList.Add(p[i]);
                }
                else if(((Prism)p[i]).minX > ((Prism)p[c]).maxX){ // if selected prism is not a potential collision, check for collisions for all prisms in active list
                    checkCollisions(activeList);
                    activeList.Add(p[i]);
                    for(int j = c ; j < i; j++){    // iterate and delete all items no longer a potential collision with selected prism
                        if(((Prism)p[i]).minX > ((Prism)p[j]).maxX){   
                            activeList.Remove(p[j]);
                        }
                        c = j;
                    }
                    //c += 1;
                }
               // Console.WriteLine("C = " + c);
            }

            checkCollisions(activeList);
        }

        public static void checkCollisions(ArrayList p){
            Console.WriteLine("STARTING CHECK");
            for(int i = 0; i < p.Count; i++){
                Console.WriteLine(((Prism)p[i]).minX + ", " + ((Prism)p[i]).minY + ", " + ((Prism)p[i]).maxX + ", " + ((Prism)p[i]).maxY);
            }
            Console.WriteLine("CHECK COMPLETE");
        }
    }
    
}