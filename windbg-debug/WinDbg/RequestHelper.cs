using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace windbg_debug.WinDbg
{
    public class RequestHelper
    {
        #region Fields 

        [ThreadStatic]
        private readonly IDebugAdvanced3 _advanced;

        #endregion

        #region Constructor

        public RequestHelper(IDebugAdvanced3 advanced)
        {
            if (advanced == null)
                throw new ArgumentNullException(nameof(advanced));

            _advanced = advanced;
        }

        #endregion

        #region Public Methods

        public _DEBUG_TYPED_DATA CreateTypedData(ulong modBase, ulong offset, uint typeId)
        {
            _EXT_TYPED_DATA result = new _EXT_TYPED_DATA();
            result.Operation = _EXT_TDOP.EXT_TDOP_SET_FROM_TYPE_ID_AND_U64;
            result.Flags = 0; // Virtual Memory
            result.InData = new _DEBUG_TYPED_DATA()
            {
                ModBase = modBase,
                Offset = offset,
                TypeId = typeId,
            };
            // will be populated
            result.OutData = new _DEBUG_TYPED_DATA();
            result.Status = 0;

            var response = PerformRequest(result, Defaults.NoPayload);
            return response.OutData;
        }

        public _EXT_TYPED_DATA OutputFullValue(_DEBUG_TYPED_DATA typedData)
        {
            var result = new _EXT_TYPED_DATA();
            result.Operation = _EXT_TDOP.EXT_TDOP_OUTPUT_FULL_VALUE;
            result.InData = typedData;

            // will be populated
            result.Status = 0;

            return PerformRequest(result, Defaults.NoPayload);
        }

        public _DEBUG_TYPED_DATA Evaluate(string toEvaluate)
        {
            var result = new _EXT_TYPED_DATA();
            result.Operation = _EXT_TDOP.EXT_TDOP_EVALUATE;
            result.Flags = 0;
            //result.InData =  // additional type information ...
            result.InStrIndex = (uint)Marshal.SizeOf(result);

            return PerformRequest(result, Encoding.Default.GetBytes(toEvaluate)).OutData;
        }

        public _DEBUG_TYPED_DATA Dereference(_DEBUG_TYPED_DATA typedData)
        {
            var result = new _EXT_TYPED_DATA();
            result.Operation = _EXT_TDOP.EXT_TDOP_GET_DEREFERENCE;
            result.InData = typedData;
            // will be populated
            result.Status = 0;
            // result.OutData = ...

            var response = PerformRequest(result, Defaults.NoPayload);
            return response.OutData;
        }

        public _DEBUG_TYPED_DATA GetField(_DEBUG_TYPED_DATA typedData, string field)
        {
            var result = new _EXT_TYPED_DATA();
            result.Operation = _EXT_TDOP.EXT_TDOP_GET_DEREFERENCE;
            result.InData = typedData;
            result.InStrIndex = (uint)Marshal.SizeOf(result);
            // will be populated
            result.Status = 0;
            // result.OutData = ...

            var response = PerformRequest(result, Encoding.Default.GetBytes(field));
            return response.OutData;
        }

        #endregion

        #region Private Methods

        private static byte[] ToBytes(_EXT_TYPED_DATA data, byte[] additional)
        {
            int size = Marshal.SizeOf(data);
            byte[] result = new byte[size];

            IntPtr pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, pointer, true);
            Marshal.Copy(pointer, result, 0, size);
            Marshal.FreeHGlobal(pointer);

            if (additional.Length == 0)
                return result;

            return CombineArrays(additional, result);
        }

        private static T[] CombineArrays<T>(T[] additionalInfo, T[] baseInfo)
        {
            var temp = new T[baseInfo.Length + additionalInfo.Length];
            Array.Copy(baseInfo, temp, baseInfo.Length);
            Array.Copy(additionalInfo, 0, temp, baseInfo.Length, additionalInfo.Length);

            return temp;
        }

        private static _EXT_TYPED_DATA FromBytes(byte[] data)
        {
            _EXT_TYPED_DATA result = new _EXT_TYPED_DATA();

            int size = Marshal.SizeOf(result);
            IntPtr pointer = Marshal.AllocHGlobal(size);

            Marshal.Copy(data, 0, pointer, size);

            result = (_EXT_TYPED_DATA)Marshal.PtrToStructure(pointer, result.GetType());
            Marshal.FreeHGlobal(pointer);

            return result;
        }

        private _EXT_TYPED_DATA PerformRequest(_EXT_TYPED_DATA payload, byte[] additionalPayload)
        {
            var payloadBuffer = ToBytes(payload, additionalPayload);
            var resultBuffer = new byte[Defaults.BufferSize * 50];
            int outSize;
            var hr = _advanced.Request(DEBUG_REQUEST.EXT_TYPED_DATA_ANSI, payloadBuffer, payloadBuffer.Length, resultBuffer, Defaults.BufferSize * 50, out outSize);

            return FromBytes(resultBuffer);
        }

        #endregion
    }
}
