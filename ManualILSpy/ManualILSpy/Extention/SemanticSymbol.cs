//MIT, 2016, Brezza92, EngineKit
using System;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.Decompiler;

namespace ManualILSpy.Extention
{
    enum SemanticSymbolKind
    {
        Unknown,
        Field,
        Method,
        MethodRef,
        Property,
        Indexer,

        LocalVar,
        MethodParameter
    }

    class SemanticSymbol
    {
        public SemanticSymbol(int index)
        {
            this.Index = index;
        }
        public SemanticSymbolKind Kind
        {
            get;
            set;
        }
        public int Index
        {
            get;
            set;
        }
        public int TypeNameIndex
        {
            get;
            set;
        }
        public string FullTypeName
        {
            get;
            set;
        } 
        public string FullSymbolName
        {
            get;
            set;
        }
        public object OriginalSymbol
        {
            get;
            set;
        }
    }

}