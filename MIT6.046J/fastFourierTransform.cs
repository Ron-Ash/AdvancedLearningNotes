using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class Program {
    public static List<Complex> FastFourierTransform(List<Complex> coeff) {
        int n = coeff.Count;
        if (n <= 1) return coeff;
        if ((n&(n-1)) != 0) throw new ArgumentException("FFT length must be a power of two.");
        int half = n/2;
        
        var even = new List<Complex>(half);
        var odd = new List<Complex>(half);
        for (int j = 0; j < n; j++) {
            if (j%2 == 0) even.Add(coeff[j]);
            else odd.Add(coeff[j]);
        }
        var evenResults = FastFourierTransform(even);
        var oddResults = FastFourierTransform(odd);
        
        Complex w = Complex.FromPolarCoordinates(1, -2*Math.PI/n);
        var roots = new List<Complex>(half) {Complex.One};
        for (int i = 1; i < half; i++) {
            roots.Add(Complex.Multiply(roots[roots.Count-1], w));
        }
        
        List<Complex> Output = new List<Complex>(n);
        for (int k = 0; k < half; k++) {
            Output.Add(
                Complex.Add(
                    evenResults[k], 
                    Complex.Multiply(roots[k], oddResults[k])));
        }
        for (int k = 0; k < half; k++) {
            Output.Add(
                Complex.Subtract(
                    evenResults[k], 
                    Complex.Multiply(roots[k], oddResults[k])));
        }
        return Output;
    }

    public static List<Complex> InverseFastFourierTransform(List<Complex> coeff) {
        int n = coeff.Count;
        var conjugateCoeff = coeff.Select(c => Complex.Conjugate(c)).ToList();
        var fftCoeff = FastFourierTransform(conjugateCoeff);
        return fftCoeff.Select(c => Complex.Conjugate(c)/n).ToList();
    }

    public static List<double> FastPolynomialMultiplication(
        List<double> coeffA, List<double> coeffB) {
        var n = 1;
        while (n < coeffA.Count + coeffB.Count) {
            n *= 2;
        }
        coeffA.AddRange(Enumerable.Repeat(0.0, n - coeffA.Count));
        coeffB.AddRange(Enumerable.Repeat(0.0, n - coeffB.Count));
        var fftA = FastFourierTransform(coeffA.Select(a => new Complex(a,0)).ToList());
        var fftB = FastFourierTransform(coeffB.Select(b => new Complex(b,0)).ToList());

        List<Complex> multiplied = new List<Complex>(fftA.Count);
        for (int i = 0; i < fftA.Count; i++) {
            multiplied.Add(Complex.Multiply(fftA[i],fftB[i]));
        }

        List<Complex> coeffResult = InverseFastFourierTransform(multiplied);
        return coeffResult.Select(c => c.Real).ToList();
    }
}