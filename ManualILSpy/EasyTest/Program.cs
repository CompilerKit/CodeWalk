using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTest
{
    delegate void SimpleDel(int a);
    class Program
    {
        static void Main(string[] args)
        {
            int b = 20;
            var a = new SimpleDel(b1 =>
            {
                Console.WriteLine("OKOK" + b1);
            });
            a(b);

        }
    }
}
