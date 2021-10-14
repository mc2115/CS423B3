using System;
using System.Collections;

namespace Sort_and_Sweep_Test{
    public class Sort_and_Sweep{
        public static void Main(String[] args){ //main method to test
            var prismList = new ArrayList(); //test arraylist
            prismList.Add(new Prism(0,0,1,1)); //just initializing the list w some values

            //need to implement a sorting algorithm, needs to be semi efficient

            sweep(prismList); //testing sweep algorithm


        }

        public static ArrayList sort(ArrayList p){
            return p;
        }

        public static void sweep(ArrayList p){
            Console.WriteLine(((Prism)p[0]).minX);
            var activeList = new ArrayList();

            int c = 0;

            for(int i = 0; i < p.Count; i++){ // base case, first item put in
                if(i == 0){
                    activeList.Add(p[i]);
                }
                else if(((Prism)p[i]).minX <= ((Prism)p[c]).maxX){  // if selected prism has potential collision, add to active list
                    activeList.Add(p[i]);
                }
                else if(((Prism)p[i]).minX > ((Prism)p[c]).maxX){ // if selected prism is not a potential collision, check for collisions for all prisms in active list
                    checkCollisions(activeList);

                    for(int j = c ; j < i; j++){    // iterate and delete all items no longer a potential collision with selected prism
                        if(((Prism)p[i]).minX > ((Prism)p[j]).maxX){   
                            activeList.Remove(p[j]);
                            c = j;
                        }
                    }
                }
            }
        }

        public static void checkCollisions(ArrayList p){

        }
    }
    
}