using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace AmazingMandelbrot
{
    class PolynomialParser
    {
        enum NodeType
        {
            Number,
            Variable,
            Operation
        }
        enum OperationType
        {
            None,
            Add,
            Sub,
            Mult,
        }
        struct PolynomialNode
        {
            public Complex Scalar;
            public string Variables;

            public PolynomialNode(Complex scalar, string variables)
            {
                Scalar = scalar;
                Variables = variables;
            }
        }
        class Node
        {
            public char Var;
            public double Num;
            public NodeType Type;
            public OperationType OpType;
            public Node N1;
            public Node N2;

            public Node(double num)
            {
                Num = num;
                Var = ' ';
                Type = NodeType.Number;
                OpType = OperationType.None;
            }
            public Node(char var)
            {
                Num = 0;
                Var = var;
                Type = NodeType.Variable;
                OpType = OperationType.None;
            }
            public Node(OperationType opType, Node n1, Node n2)
            {
                Num = 0;
                Var = ' ';
                Type = NodeType.Operation;
                OpType = opType;
                N1 = n1;
                N2 = n2;
            }
        }
        public string InputString = "z^2+c";
        public int CursorPos = 5;
        public bool InsertMode = false;
        //readonly char[] Superscripts = { '²','³','' };
        public string ErrorMessage = "";
        List<PolynomialNode> PolynomialList;
        public Complex[,] CoefficientArray;
        public bool Success = true;
        
        public PolynomialParser()
        {
            Parse();
        }
        public void InputChar(char C)
        {
            if(C>='0'&&C<='9'||C=='z'||C=='c'||C=='i'||C=='('||C==')'||C=='^'||C=='.'||C=='+' || C == '-' || C == '*')
            {
                if (InsertMode&& CursorPos< InputString.Length)
                {
                    InputString = InputString.Remove(CursorPos, 1);
                }
                InputString = InputString.Insert(CursorPos, C.ToString());
                CursorPos++;
            }
        }
        public void InputKey(Keys K)
        {
            if(K==Keys.Left&& CursorPos>0)
            {
                CursorPos--;
            }
            if (K == Keys.Right&& CursorPos< InputString.Length)
            {
                CursorPos++;
            }
            if(K == Keys.Insert)
            {
                InsertMode = !InsertMode;
            }
            if (K==Keys.Back && CursorPos > 0)
            {
                CursorPos--;
                InputString = InputString.Remove(CursorPos, 1);
            }
            if(K==Keys.Enter)
            {
                Parse();
            }
        }
        public void Parse()
        {
            ErrorMessage = "";
            Success = true;
            string S = InputString;
            for (int i = 0; i < S.Length; i++)
            {
                if(S[i]=='^')
                {
                    if (i + 1 < S.Length)
                    {
                        if (int.TryParse(S[i + 1].ToString(), out int num))
                        {
                            S = S.Remove(i, 2);
                            string S2 = "";
                            for (int j = 0; j < num - 1; j++)
                            {
                                S2 += "*" + S[i - 1].ToString();
                            }
                            S = S.Insert(i, S2);
                        }
                        else
                        {
                            ErrorMessage = "Unexpected " + S[i + 1].ToString();
                            Success = false;
                        }
                    }else
                    {
                        ErrorMessage = "^ has no number";
                        Success = false;
                    }
                }
            }
            Node N = null;
            if (Success)
                N = ParseExpression(S);
            if (Success)
            {
                PolynomialList = AddFromTree(N);
                int MaxZ = 0;
                int MaxC = 0;
                foreach (PolynomialNode item in PolynomialList)
                {
                    int LocalZ = 0;
                    int LocalC = 0;
                    foreach (char C in item.Variables)
                    {
                        if (C == 'c')
                        {
                            LocalC++;
                        }
                        else if (C == 'z')
                        {
                            LocalZ++;
                        }
                    }
                    if (LocalC > MaxC)
                    {
                        MaxC = LocalC;
                    }
                    if (LocalZ > MaxZ)
                    {
                        MaxZ = LocalZ;
                    }
                }
                CoefficientArray = new Complex[MaxZ + 1, MaxC + 1];
                foreach (PolynomialNode item in PolynomialList)
                {
                    int LocalZ = 0;
                    int LocalC = 0;
                    foreach (char C in item.Variables)
                    {
                        if (C == 'c')
                        {
                            LocalC++;
                        }
                        else if (C == 'z')
                        {
                            LocalZ++;
                        }
                    }
                    CoefficientArray[LocalZ, LocalC] += item.Scalar;
                }
                Success = false;
                for (int i = 0; i < CoefficientArray.GetLength(1); i++)
                {
                    if(CoefficientArray[0,i].MagSq() > 0.1)
                    {
                        Success = true;
                    }
                }
                if(!Success)
                {
                    ErrorMessage = "Fractal is singular";
                }
            }
        }
        List<PolynomialNode> AddFromTree(Node N)
        {
            switch (N.Type)
            {
                case NodeType.Number:
                    return new List<PolynomialNode>() { new PolynomialNode(new Complex(N.Num, 0), "") };
                case NodeType.Variable:
                    if(N.Var=='i')
                    {
                        return new List<PolynomialNode>() { new PolynomialNode(new Complex(0, 1), "") };
                    }
                    else
                    {
                        return new List<PolynomialNode>() { new PolynomialNode(new Complex(1, 0), N.Var.ToString()) };
                    }
                case NodeType.Operation:
                    {
                        List<PolynomialNode> L1 = AddFromTree(N.N1);
                        List<PolynomialNode> L2 = AddFromTree(N.N2);
                        List<PolynomialNode> L3 = new List<PolynomialNode>();
                        if (N.OpType== OperationType.Mult)
                        {
                            for (int i = 0; i < L1.Count; i++)
                            {
                                for (int j = 0; j < L2.Count; j++)
                                {
                                    L3.Add(new PolynomialNode(L1[i].Scalar * L2[j].Scalar,L1[i].Variables+L2[j].Variables));
                                }
                            }
                            
                        }
                        else
                        {
                            double M = 1;
                            if(N.OpType== OperationType.Sub)
                            {
                                M = -1;
                            }
                            for (int i = 0; i < L1.Count; i++)
                            {
                                L3.Add(L1[i]);
                            }
                            for (int i = 0; i < L2.Count; i++)
                            {
                                L3.Add(new PolynomialNode(L2[i].Scalar*new Complex(M,0),L2[i].Variables));
                            }
                        }
                        return L3;
                    }
                default:
                    return new List<PolynomialNode>();
            }
        }
        Node ParseExpression(string S)
        {
            if (S.Length == 0)
                return new Node(0);
            int Index = S.Length-1;
            int Layer = 0;
            while(Index>=0)
            {
                if (Index >= S.Length)
                {
                    ErrorMessage = "Unmatched (";
                    Success = false;
                    return null;
                }
                if (S[Index] =='(')
                {
                    Layer++;
                }
                if(S[Index] == ')')
                {
                    Layer--;
                }
                if(Layer==0&&(GetOperation(S[Index])== OperationType.Add|| GetOperation(S[Index]) == OperationType.Sub))
                {
                    OperationType Op = GetOperation(S[Index]);
                    string A = S.Substring(0,Index);
                    string B = S.Substring(Index+1);
                    return new Node(Op, ParseExpression(A), ParseExpression(B));
                }
                Index--;
            }

            if (S[0] == '(')
            {
                int MatchPos = 0;
                Layer = 1;
                while (MatchPos < S.Length&& Layer > 0)
                {
                    MatchPos++;
                    if (MatchPos >= S.Length)
                    {
                        ErrorMessage = "Unmatched (";
                        Success = false;
                        return null;
                    }
                    if (S[MatchPos] == '(')
                    {
                        Layer++;
                    }else
                    if (S[MatchPos] == ')')
                    {
                        Layer--;
                    }
                }
                string A = S.Substring(1, MatchPos-1);

                if (MatchPos<S.Length-1)
                {
                    string B = S.Substring(MatchPos+1);
                    OperationType Op = GetOperation(B[0]);
                    if (Op == OperationType.None)
                    {
                        Op = OperationType.Mult;
                        return new Node(Op, ParseExpression(A), ParseExpression(B));
                    }
                    return new Node(Op, ParseExpression(A), ParseExpression(B.Substring(1)));
                }
                return ParseExpression(A);

            }
            else if (S[0] >= '0' && S[0] <= '9')
            {
                string N = S[0].ToString();
                int i = 1;
                while (i < S.Length && (S[i] >= '0' && S[i] <= '9' || S[i] == '.'))
                {
                    N += S[i];
                    i++;
                }
                double Number;
                N = N.Replace('.', ',');
                if (double.TryParse(N, out Number))
                {
                    if (i < S.Length)
                    {
                        OperationType Op = GetOperation(S[i]);
                        if (Op == OperationType.None)
                        {
                            Op = OperationType.Mult;
                            return new Node(Op, new Node(Number), ParseExpression(S.Substring(i)));
                        }
                        return new Node(Op, new Node(Number), ParseExpression(S.Substring(i + 1)));
                    }
                    else
                    {
                        return new Node(Number);
                    }
                }
                else
                {
                    ErrorMessage = N + " could not be paresd into a number";
                    Success = false;
                    return null;
                }
            }
            else if (S[0] == 'c' || S[0] == 'z' || S[0] == 'i')
            {
                if (S.Length > 1)
                {
                    OperationType Op = GetOperation(S[1]);
                    if (Op == OperationType.None)
                    {
                        Op = OperationType.Mult;
                        return new Node(Op, new Node(S[0]), ParseExpression(S.Substring(1)));
                    }
                    return new Node(Op, new Node(S[0]), ParseExpression(S.Substring(2)));
                }
                else
                {
                    return new Node(S[0]);
                }
            }
            else
            {
                Success = false;
                ErrorMessage = "Unexpected " + S[0];
                return null;
            }
        }
        OperationType GetOperation(char C)
        {
            switch (C)
            {
                case '+': return OperationType.Add;
                case '-': return OperationType.Sub;
                case '*': return OperationType.Mult;
                default:return OperationType.None;
            }
        }

    }
}
