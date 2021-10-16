using System;
using System.Collections;

namespace Sort_and_Sweep_Test
{
    public class MergeSort
    {
        public void merge(ArrayList p, int l, int m, int r){
            int n1 = m - l + 1;
            int n2 = r - m;
            // Create temp arrays
            Prism[] L = new Prism[n1];
            Prism[] R = new Prism[n2];
            int i, j;
    
            // Copy data to temp arrays
            for (i = 0; i < n1; ++i)
                L[i] = (Prism) p[l + i];
            for (j = 0; j < n2; ++j)
                R[j] = (Prism)p[m + 1 + j];
    
            // Merge the temp arrays
    
            // Initial indexes of first
            // and second subarrays
            i = 0;
            j = 0;
    
            // Initial index of merged
            // subarray array
            int k = l;
            while (i < n1 && j < n2) {
                if (((Prism)L[i]).minX <= ((Prism)R[j]).minX) {
                    p[k] = L[i];
                    i++;
                }
                else {
                    p[k] = R[j];
                    j++;
                }
                k++;
            }
    
            // Copy remaining elements
            // of L[] if any
            while (i < n1) {
                p[k] = L[i];
                i++;
                k++;
            }
    
            // Copy remaining elements
            // of R[] if any
            while (j < n2) {
                p[k] = R[j];
                j++;
                k++;
            }
        }

        public void sort(ArrayList p, int l, int r){
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
    }
}