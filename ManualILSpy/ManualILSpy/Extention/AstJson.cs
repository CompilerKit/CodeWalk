using System;
using System.Collections.Generic;
using System.ComponentModel;
 
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mono.Cecil;
using ICSharpCode.ILSpy;
using ICSharpCode.Decompiler;
using ManualILSpy.Extention;

namespace AstJson
{
    class AstJsonWriter
    {
        public void Write(string header, string text)
        {

        }
    }

    class AstJsonReader
    {

    }

    abstract class AstJsonNode
    {
        AstJsonNode _prevNode;
        AstJsonNode _nextNode;
        abstract public void ParseJson(AstJsonReader reader);
        abstract public void WriteJson(AstJsonWriter writer);
    }

    class ExpressionNode : AstJsonNode
    {
        public override void ParseJson(AstJsonReader reader)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(AstJsonWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class StatementNode : AstJsonNode
    {
        public override void ParseJson(AstJsonReader reader)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(AstJsonWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}