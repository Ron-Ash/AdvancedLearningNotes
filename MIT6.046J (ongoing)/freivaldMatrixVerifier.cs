using System;
using System.Numerics;

public class Program {
    Random rand = new Random();
    public bool SingleRoundVerifier(double[,] A, double[,] B, double[,] C, double tol) {
        double[,] r = new double[B.GetLength(1), 1];
        for (int i = 0; i < B.GetLength(1); i++) r[i, 0] = rand.Next(2);

        double[,] tempBr = new double[B.GetLength(0), 1];
        double[,] tempABr = new double[A.GetLength(0), 1];
        double[,] tempCr = new double[C.GetLength(0), 1];

        for (int i = 0; i < B.GetLength(0); i++) {
            double temp = 0;
            for (int j = 0; j < B.GetLength(1); j++) temp += B[i, j]*r[j, 0];
            tempBr[i, 0] = temp;
        }
        for (int i = 0; i < A.GetLength(0); i++) {
            double temp = 0;
            for (int j = 0; j < A.GetLength(1); j++) temp += A[i, j]*tempBr[j, 0];
            tempABr[i, 0] = temp;
        }

        for (int i = 0; i < C.GetLength(0); i++) {
            double temp = 0;
            for (int j = 0; j < C.GetLength(1); j++) temp += C[i, j]*r[j, 0];
            tempCr[i, 0] = temp;
        }

        for (int i = 0; i < A.GetLength(0); i++) if (Math.Abs(tempABr[i, 0] - tempCr[i, 0]) > tol) return false;
        return true;
    }
    public bool FreivaldMatrixVerifier(double[,] A, double[,] B, double[,] C, double falsePositiveThreshold=0.01f, double tol = 1e-9) {
        if (A.GetLength(1) != B.GetLength(0) || A.GetLength(0) != C.GetLength(0) || B.GetLength(1) != C.GetLength(1)) return false;

        double falsePositive = 1.0f;
        while (falsePositive>falsePositiveThreshold) {
            if (!SingleRoundVerifier(A,B,C, tol)) return false;
            falsePositive /= 2.0f;
        }
        return true;
    }
}