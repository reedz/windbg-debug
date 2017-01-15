using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace windbg_debug.WinDbg.Data
{
    public class TypedVariable
    {
        #region Constructor

        public TypedVariable(_DEBUG_TYPED_DATA data, _DEBUG_TYPED_DATA dereferenced, IDictionary<string, TypedVariable> fields)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));

            Fields = new ReadOnlyDictionary<string, TypedVariable>(fields);
            Data = data;
            Dereferenced = dereferenced;
        }

        #endregion

        #region Public Properties

        public ReadOnlyDictionary<string, TypedVariable> Fields { get; private set; }
        public _DEBUG_TYPED_DATA Data { get; private set; }
        public _DEBUG_TYPED_DATA Dereferenced { get; private set; }

        #endregion
    }
}
