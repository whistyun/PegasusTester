using System;

namespace ExamplePegProject
{
    public class Command { }

    public class Operator2 : Command
    {
        public Command Left { get; set; }
        public string Operator { set; get; }
        public Command Right { get; set; }

        public Operator2(Command l, string op, Command r)
        {
            Left = l;
            Operator = op;
            Right = r;
        }
    }

    public class SingleValue : Command
    {
        public string Value { get; set; }

        public SingleValue(string vl)
        {
            Value = vl;
        }
    }

    public class Operator1 : Command
    {
        public string Operator { set; get; }
        public Command One { get; set; }

        public Operator1(string op, Command one)
        {
            Operator = op;
            One = one;
        }
    }

    public class Brace : Operator1
    {
        public Brace(Command op) : base("()", op)
        {
        }
    }
}
