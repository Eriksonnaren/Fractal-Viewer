using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazingMandelbrot
{
    class FractalMath
    {
        public Complex[,] CoefficientArray;
        public int MaxPeriod = 50;
        Complex[] PolynomialConstants;
        public int FindPeriod(Complex C)
        {
            PreloadCoefficients(C);
            for (int p = 1; p < MaxPeriod; p++)
            {
                Complex Z0 = C;
                for (int i = 0; i < 20; i++)//newtons method
                {
                    Complex Z1 = Z0;
                    Complex DZ1 = new Complex(1, 0);
                    for (int j = 0; j < p; j++)
                    {
                        DZ1 = ComputeDerivative(Z1) * DZ1;
                        Z1 = Compute(Z1);
                    }
                    Z0 -= (Z1 - Z0) / (DZ1 - new Complex(1, 0));
                }
                //Complex FixedPoint = Z0;
                Complex DZ0 = new Complex(1, 0);
                for (int j = 0; j < p; j++)
                {
                    DZ0 = ComputeDerivative(Z0) * DZ0;
                    Z0 = Compute(Z0);
                }
                if (DZ0.MagSq() < 1.0)
                {
                    return p;
                }
            }
            return 0;
        }
        public void PreloadCoefficients(Complex C)
        {
            PolynomialConstants = new Complex[CoefficientArray.GetLength(0)];
            Complex PowerC = new Complex(1,0);
            for (int j = 0; j < CoefficientArray.GetLength(1); j++)
            {
                for (int i = 0; i < CoefficientArray.GetLength(0); i++)
                {
                    PolynomialConstants[i] += PowerC*CoefficientArray[i,j];
                }
                PowerC *= C;
            }
        }
        public Complex Compute(Complex Z)
        {
            Complex NewZ = new Complex(0, 0);
            for (int i = CoefficientArray.GetLength(0) - 1; i >= 0; i--)
            {
                NewZ = Z* NewZ + PolynomialConstants[i];
            }
            return NewZ;
        }
        public Complex ComputeDerivative(Complex Z)
        {
            Complex NewZ = new Complex(0, 0);
            for (int i = CoefficientArray.GetLength(0) - 1; i >= 1; i--)
            {
                NewZ = Z * NewZ + PolynomialConstants[i]*i;
            }
            return NewZ;
        }
    }
}
