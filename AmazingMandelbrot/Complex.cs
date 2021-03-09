using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazingMandelbrot
{
    public struct Complex
    {

        public double real, imag;
        public Complex(double real, double imag)
        {
            this.real = real;
            this.imag = imag;
        }

        public static Complex operator +(Complex C1, Complex C2)
        {
            return new Complex(C1.real + C2.real, C1.imag + C2.imag);
        }
        public static Complex operator +(Complex C1, double d)
        {
            return new Complex(C1.real + d, C1.imag);
        }
        public static Complex operator +(double d, Complex c1)
        {
            return new Complex(c1.real + d, c1.imag);
        }
        public static Complex operator -(Complex C1, Complex C2)
        {
                return new Complex(C1.real - C2.real, C1.imag - C2.imag);
        }
        public static Complex operator -(Complex C1, double d)
        {
            return new Complex(C1.real - d, C1.imag);
        }
        public static Complex operator -(Complex C1)
        {
            return new Complex(-C1.real, -C1.imag);
        }
        public static Complex operator -(double d, Complex C1)
        {
            return new Complex(d-C1.real, -C1.imag);
        }
        public Complex Sin()
        {
            return new Complex(Math.Sin(real) * Math.Cosh(imag), Math.Cos(real) * Math.Sinh(imag));
        }
        public Complex Cos()
        {
            return new Complex(Math.Cos(real) * Math.Cosh(imag), Math.Sin(real) * -Math.Sinh(imag));
        }
        public Complex Pow(Complex c2)
        {
           
                if (real == 0 && imag == 0)
                {
                    return new Complex(0, 0);
                }
                double Arg = Math.Atan2(imag, real);

                double P1 = Math.Pow(real * real + imag * imag, c2.real / 2) * Math.Exp(-c2.imag * Arg);
                double P2 = c2.real * Arg + 0.5 * c2.imag * Math.Log(real * real + imag * imag);

                Complex R = new Complex(P1 * Math.Cos(P2), P1 * Math.Sin(P2));
                return R;
            
        }
        public Complex Ln()
        {
            return new Complex(Math.Log(Math.Sqrt(real * real + imag * imag)), Math.Atan2(imag, real));
        }
        public Complex Exp()
        {
            double M = Math.Exp(real);
            return new Complex(M * Math.Cos(imag), M * Math.Sin(imag));
        }
        public Complex Pow(double c2)
        {
            
                double Arg = Math.Atan2(imag, real);
                double Rad = Math.Pow(real * real + imag * imag, c2 / 2);
                double P2 = c2 * Arg;
                return new Complex(Rad * Math.Cos(P2), Rad * Math.Sin(P2));
            
        }
        public Complex Sqrt()
        {

            
            double Radius = Math.Sqrt(real * real + imag * imag);
            if(imag<0)
                return new Complex(Math.Sqrt((Radius + real) / 2), -Math.Sqrt((Radius - real) / 2));
            else
                return new Complex(Math.Sqrt((Radius + real) / 2), Math.Sqrt((Radius - real) / 2));
            //double Arg = Math.Atan2(imag, real);
            //return new Complex(Math.Sqrt(Radius) * Math.Cos(Arg / 2), Math.Sqrt(Radius) * Math.Sin(Arg / 2));
        }
        public static Complex operator *(Complex c1, Complex c2)
        {
                return new Complex((c1.real * c2.real) - (c1.imag * c2.imag), (c1.real * c2.imag) + (c2.real * c1.imag));
        }
        public static Complex operator *(Complex c1, double d)
        {

            return new Complex(c1.real * d , c1.imag*d );

        }
        public static Complex operator *(double d, Complex c1)
        {

            return new Complex(c1.real * d, c1.imag * d);

        }
        
        public static Complex operator /(Complex c1, Complex c2)
        {
            
                if (c2.real == 0 && c2.imag == 0)
                    return new Complex(0, 0);
                double Divisor = c2.real * c2.real + c2.imag * c2.imag;
                Complex Out = new Complex((c1.real * c2.real + c1.imag * c2.imag) / Divisor,
                                   (c1.imag * c2.real - c1.real * c2.imag) / Divisor);
                return Out;
           
        }
        public static Complex operator /(Complex c1, double d)
        {

            if (d == 0)
                return new Complex(0, 0);
            double Divisor = d;
            Complex Out = new Complex(c1.real / d, c1.imag / d);
            return Out;

        }
        public double MagSq()
        {
            return real * real + imag * imag;
        }
        public double Mag()
        {
            return Math.Sqrt(MagSq());
        }
        public static Complex Parse(string S)
        {
            S = S.Replace('.', ',');
            if (S[S.Length - 1] == ',')
                S = S.Remove(S.Length - 1);
            return new Complex(double.Parse(S), 0);
        }
        public override string ToString()
        {
            return real.ToString()+"+"+ imag.ToString() +"i";
        }
        public bool Equals(Complex C)
        {
            return C.real == real && C.imag == imag;
        }
        public static implicit operator Complex(double d)
        {
            return new Complex(d, 0);
        }
    }
}
