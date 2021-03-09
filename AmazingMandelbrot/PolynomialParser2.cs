using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AmazingMandelbrot
{
    class PolynomialParser2
    {
        public delegate void ParseComplete();
        public ParseComplete parseEvent;
        public string InputString = "z^2+c";
        public int CursorPos = 5;
        public bool InsertMode = false;
        public string ErrorMessage = "";
        public List<PolynomialNode> PolynomialList;
        public List<char> Variables=new List<char>();
        public bool Success = true;
        enum Operator
        {
            None,
            Multiply,
            Add,
            Subtract,
            Power
        }
        enum TokenKind
        {
            Number,
            Variable,
            Operator,
            Parenthesis,
            ClosingParenthesis
        }
        class Token
        {
            public TokenKind kind;
            public Operator operator_type;
            public double num;
            public char var;
            public string original;
            public Token(char a)
            {
                original = a.ToString();
                operator_type = Operator.None;
                kind = TokenKind.Operator;
                num = 0;
                var = (char)0;
                switch (a)
                {
                    case '(': kind = TokenKind.Parenthesis; break;
                    case ')': kind = TokenKind.ClosingParenthesis; break;
                    case '*': kind = TokenKind.Operator; operator_type = Operator.Multiply; break;
                    case '+': kind = TokenKind.Operator; operator_type = Operator.Add; break;
                    case '-': kind = TokenKind.Operator; operator_type = Operator.Subtract; break;
                    case '^': kind = TokenKind.Operator; operator_type = Operator.Power; break;
                    default: kind = TokenKind.Variable; var = a; break;
                }
            }
            public Token(double n)
            {
                original = n.ToString();
                operator_type = Operator.None;
                kind = TokenKind.Number;
                num = n;
                var = (char)0;
            }
        }
        class TokenStream
        {
            List<Token> Tokens;
            int Count;
            public TokenStream(List<Token> tokens)
            {
                Tokens = tokens;
                Count = 0;
            }
            public Token peek()
            {
                if (Count >= Tokens.Count)
                    return null;
                return Tokens[Count];
            }
            public Token consume()
            {
                if (Count >= Tokens.Count)
                    return null;
                return Tokens[Count++];
            }
        }
        enum NodeType
        {
            Number,
            Variable,
            Operation
        }
        class Node
        {
            public char Var;
            public double Num;
            public NodeType Type;
            public Operator OpType;
            public Node N1;
            public Node N2;

            public Node(double num)
            {
                Num = num;
                Var = ' ';
                Type = NodeType.Number;
                OpType = Operator.None;
            }
            public Node(char var)
            {
                Num = 0;
                Var = var;
                Type = NodeType.Variable;
                OpType = Operator.None;
            }
            public Node(Operator opType, Node n1, Node n2)
            {
                Num = 0;
                Var = ' ';
                Type = NodeType.Operation;
                OpType = opType;
                N1 = n1;
                N2 = n2;
            }
        }
        public struct PolynomialNode
        {
            public Complex Scalar;
            public SortedDictionary<int, int> Variables;

            public PolynomialNode(Complex scalar, int variable,int exponent)
            {
                Scalar = scalar;
                Variables = new SortedDictionary<int, int>(){ { variable, exponent } };
            }
            public PolynomialNode(Complex scalar, SortedDictionary<int, int> variables)
            {
                Scalar = scalar;
                Variables = variables;
            }
            public PolynomialNode(Complex scalar)
            {
                Scalar = scalar;
                Variables = new SortedDictionary<int, int>();
            }
            public bool Matches(PolynomialNode N)
            {
                if (N.Variables.Count != Variables.Count)
                    return false;
                for (int i = 0; i < N.Variables.Count; i++)
                {
                    if(!N.Variables.ElementAt(i).Equals(Variables.ElementAt(i)))
                    {
                        return false;
                    }
                }
                return true;
            }
            public void Add(PolynomialNode N)
            {
                Scalar += N.Scalar;
            }
        }

        public PolynomialParser2()
        {
            Variables.Add('z');
            Parse();
        }
        public void InputChar(char C)
        {
            if (C >= '0' && C <= '9' || C >= 'a' && C <= 'z'|| C == '(' || C == ')' || C == '^' || C == '.' || C == '+' || C == '-' || C == '*')
            {
                if (InsertMode && CursorPos < InputString.Length)
                {
                    InputString = InputString.Remove(CursorPos, 1);
                }
                InputString = InputString.Insert(CursorPos, C.ToString());
                CursorPos++;
            }
        }
        public void InputKey(Keys K)
        {
            if (K == Keys.Left && CursorPos > 0)
            {
                CursorPos--;
            }
            if (K == Keys.Right && CursorPos < InputString.Length)
            {
                CursorPos++;
            }
            if (K == Keys.Insert)
            {
                InsertMode = !InsertMode;
            }
            if (K == Keys.Back && CursorPos > 0)
            {
                CursorPos--;
                InputString = InputString.Remove(CursorPos, 1);
            }
            if (K == Keys.Enter)
            {
                Parse();
            }
        }
        public void Parse()
        {
            
            var Tokens = Tokenize(InputString);
            Console.WriteLine("Input:{0}", InputString);
            var Tokenstream = new TokenStream(Tokens);
            var parsedNode = parse_expression(Tokenstream);
            PolynomialList = AddFromTree(parsedNode);
            bool[] ExistingVariables = new bool[Variables.Count];
            for (int i = 0; i < PolynomialList.Count; i++)
            {
                Console.Write(PolynomialList[i].Scalar.ToString());
                for (int j = 0; j < PolynomialList[i].Variables.Count; j++)
                {
                    int varid = PolynomialList[i].Variables.ElementAt(j).Key;
                    Console.Write(" {0}^{1}", Variables[varid].ToString(), PolynomialList[i].Variables.ElementAt(j).Value.ToString());
                    ExistingVariables[varid] = true;
                }
                Console.WriteLine();
            }
            
            for (int i = Variables.Count-1; i >=0; i--)
            {
                if (!ExistingVariables[i])
                    Variables.RemoveAt(i);

            }
            for (int i = 0; i < Variables.Count; i++)
            {
                Console.Write(Variables[i].ToString() + " ");

            }

            Console.WriteLine();
            if (Success)
                parseEvent?.Invoke();
        }
        List<Token> Tokenize(string input)
        {
            List<Token> Tokens = new List<Token>();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] >= '0' && input[i] <= '9')
                {
                    string N = input[i].ToString();
                    int j = i+1;
                    while (j < input.Length && (input[j] >= '0' && input[j] <= '9' || input[j] == '.'))
                    {
                        N += input[j];
                        j++;
                    }
                    i = j - 1;
                    double Number;
                    N = N.Replace('.', ',');
                    if (double.TryParse(N, out Number))
                    {
                        Tokens.Add(new Token(Number));
                    }
                    else
                    {
                        //invalid number
                    }

                }else
                {
                    Tokens.Add(new Token(input[i]));
                }
            }
            return Tokens;
        }
        Node parse_value(TokenStream tokens)
        {
            Token token = tokens.consume();
            switch (token.kind)
            {
                case TokenKind.Number:
                    return new Node(token.num);
                case TokenKind.Parenthesis:
                    Node expression = parse_expression(tokens);
                    if (tokens.consume().kind != TokenKind.ClosingParenthesis)
                    {
                        // Handle error somehow! Not closed parenthesis
                    }
                    return expression;
                case TokenKind.Variable:
                    return new Node(token.var);
                /* more cases here */
                default:
                    // Error! Not a value
                    return new Node(0);
            }
        }
        Node parse_expression(TokenStream tokens, int min_precedence = 0)
        {
            //Console.WriteLine("start parse_expression with first token: {0} and min_precedence: {1}", tokens?.peek()?.original, min_precedence);
            Node value = parse_value(tokens);

            if (value == null) return null;

            while (true)
            {
                if (tokens.peek() == null)
                {
                    //Console.WriteLine("token does not exist");
                    break;
                }
                if (tokens.peek().kind == TokenKind.ClosingParenthesis)
                {
                    //Console.WriteLine("found ')'");
                    break;
                }
                bool consumeOperator = true;
                Operator op_type = tokens.peek().operator_type;
                if (op_type == Operator.None)
                {
                    //Console.WriteLine("convert ) into multiplication");
                    consumeOperator = false;
                    op_type = Operator.Multiply;
                }
                int precedence = get_operator_precedence(op_type);
                if (operator_is_right_to_left(op_type))
                {
                    if (precedence < min_precedence)
                    {
                        //Console.WriteLine("operator {0} has lower precedence than min_precedence", op_type.ToString());
                        break;
                    }
                }
                else
                {
                    if (precedence <= min_precedence)
                    {
                        //Console.WriteLine("operator {0} has lower precedence({1}) than min_precedence({2})", op_type.ToString(), precedence, min_precedence);
                        break;
                    }
                }
                if (consumeOperator)
                {
                    //Console.WriteLine("consumed token {0}", tokens.peek().original);
                    tokens.consume();
                }
                //Console.WriteLine("recursivly parse again");
                Node next = parse_expression(tokens, precedence);

                if (next == null) return null;

                value = new Node(op_type, value, next);
            }
            return value;
        }
        int get_operator_precedence(Operator op)
        {
            switch (op)
            {
                case Operator.None:return 0;
                case Operator.Multiply: return 3;
                case Operator.Add:return 1;
                case Operator.Subtract:return 2;
                case Operator.Power:return 4;
                default: return 0;
            }
        }
        bool operator_is_right_to_left(Operator op)
        {
            return false;
        }
        List<PolynomialNode> AddFromTree(Node N)
        {
            switch (N.Type)
            {
                case NodeType.Number:
                    return new List<PolynomialNode>() { new PolynomialNode(new Complex(N.Num, 0)) };
                case NodeType.Variable:
                    if (N.Var == 'i')
                    {
                        return new List<PolynomialNode>() { new PolynomialNode(new Complex(0, 1)) };
                    }
                    else
                    {
                        int index = Variables.IndexOf(N.Var);
                        if (index == -1)
                        {
                            index = Variables.Count;
                            Variables.Add(N.Var);
                        }
                        return new List<PolynomialNode>() { new PolynomialNode(new Complex(1, 0), index, 1) };
                    }
                case NodeType.Operation:
                    {
                        List<PolynomialNode> L1 = AddFromTree(N.N1);
                        List<PolynomialNode> L2 = AddFromTree(N.N2);
                        List<PolynomialNode> L3 = new List<PolynomialNode>();
                        if (N.OpType == Operator.Multiply)
                        {
                            for (int i = 0; i < L1.Count; i++)
                            {
                                for (int j = 0; j < L2.Count; j++)
                                {

                                    var node = new PolynomialNode(L1[i].Scalar* L2[j].Scalar);
                                    node.Variables = CopyDictionary(L1[i].Variables);
                                    MergeDictionaries(node.Variables, L2[j].Variables,L3);
                                    AddNodeToList(node,L3);
                                }
                            }

                        }
                        else if(N.OpType == Operator.Power)
                        {
                            if (N.N2.Type == NodeType.Number && N.N2.Num >= 1)
                            {
                                int num = (int)N.N2.Num;
                                int[] arr = new int[L1.Count];
                                //loop through all combinations of positive array contents that add up to num
                                while (Increment(arr, num))
                                {
                                    //gets multinomial coefficients
                                    int IntCoefficient = Factorial(num);
                                    Complex ComplexCoefficient = new Complex(1, 0);
                                    SortedDictionary<int, int> D = new SortedDictionary<int, int>();
                                    for (int i = 0; i < arr.Length; i++)
                                    {
                                        IntCoefficient /= Factorial(arr[i]);
                                        for (int j = 0; j < arr[i]; j++)
                                        {
                                            ComplexCoefficient *= L1[i].Scalar;
                                        }
                                        if(arr[i]>0)
                                            MergeDictionaries(D,L1[i].Variables, L3, arr[i]);
                                    }

                                    AddNodeToList(new PolynomialNode(IntCoefficient* ComplexCoefficient, D), L3);
                                }
                            }
                            else
                            {
                                //shit
                            }
                        }
                        else
                        {
                            double M = 1;
                            if (N.OpType == Operator.Subtract)
                            {
                                M = -1;
                            }
                            for (int i = 0; i < L1.Count; i++)
                            {
                                L3.Add(L1[i]);
                            }
                            for (int i = 0; i < L2.Count; i++)
                            {
                                AddNodeToList(new PolynomialNode(L2[i].Scalar * new Complex(M, 0), L2[i].Variables),L3);
                            }
                        }
                        return L3;
                    }
                default:
                    return new List<PolynomialNode>();
            }
        }
        void MergeDictionaries(SortedDictionary<int, int> V1, SortedDictionary<int, int> V2, List<PolynomialNode> L, int Scale = 1)
        {
            for (int k = 0; k < V2.Count; k++)
            {
                int key = V2.Keys.ElementAt(k);
                if (V1.ContainsKey(key))
                {
                    V1[key] += V2[key]*Scale;
                }
                else
                {
                    V1.Add(V2.ElementAt(k).Key, V2.ElementAt(k).Value*Scale);
                }
            }
        }
        void AddNodeToList(PolynomialNode node, List<PolynomialNode> L)
        {

            for (int i = 0; i < L.Count; i++)
            {
                if (node.Matches(L[i]))
                {
                    L[i].Add(node);
                    return;
                }
            }
            L.Add(node);
        }
        SortedDictionary<int, int> CopyDictionary(SortedDictionary<int, int> V1)
        {
            SortedDictionary<int, int> V2 = new SortedDictionary<int, int>();
            for (int i = 0; i < V1.Count; i++)
            {
                V2.Add(V1.ElementAt(i).Key, V1.ElementAt(i).Value);
            }
            return V2;
        }
        int Factorial(int n)
        {
            int f = 1;
            for (int i = 1; i <= n; i++)
            {
                f *= i;
            }
            return f;
        }
        bool Increment(int[] arr, int sum)
        {
            while(true)
            {
                arr[0]++;
                int i = 0;
                while(arr[i] > sum)
                {
                    arr[i] = 0;
                    i++;
                    if (i == arr.Length)
                        return false;
                    arr[i]++;
                }
                int s = 0;
                for (int j = 0; j < arr.Length; j++)
                {
                    s += arr[j];
                }
                if(s==sum)
                {
                    return true;
                }
            }
        }
    }
}
