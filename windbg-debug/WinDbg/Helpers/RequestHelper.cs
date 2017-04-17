using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Helpers
{
    public class RequestHelper
    {
        [ThreadStatic]
        private readonly IDebugAdvanced3 _advanced;

        [ThreadStatic]
        private readonly IDebugDataSpaces4 _spaces;

        [ThreadStatic]
        private readonly IDebugSymbols4 _symbols;

        public RequestHelper(IDebugAdvanced3 advanced, IDebugDataSpaces4 spaces, IDebugSymbols4 symbols)
        {
            if (advanced == null)
                throw new ArgumentNullException(nameof(advanced));

            if (spaces == null)
                throw new ArgumentNullException(nameof(spaces));

            if (symbols == null)
                throw new ArgumentNullException(nameof(symbols));

            _advanced = advanced;
            _spaces = spaces;
            _symbols = symbols;
        }

        public _DEBUG_TYPED_DATA CreateTypedData(ulong modBase, ulong offset, uint typeId)
        {
            _EXT_TYPED_DATA result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_SET_FROM_TYPE_ID_AND_U64;
            result.Flags = 0; // Virtual Memory
            result.InData = new _DEBUG_TYPED_DATA()
            {
                ModBase = modBase,
                Offset = offset,
                TypeId = typeId,
            };

            // will be populated
            result.OutData = default(_DEBUG_TYPED_DATA);
            result.Status = 0;

            var response = PerformRequest(result, Defaults.NoPayload);
            return response.OutData;
        }

        public _EXT_TYPED_DATA OutputFullValue(_DEBUG_TYPED_DATA typedData)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_OUTPUT_FULL_VALUE;
            result.InData = typedData;

            // will be populated
            result.Status = 0;

            return PerformRequest(result, Defaults.NoPayload);
        }

        public _EXT_TYPED_DATA OutputShortValue(_DEBUG_TYPED_DATA typedData)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_OUTPUT_SIMPLE_VALUE;
            result.InData = typedData;

            // will be populated
            result.Status = 0;

            return PerformRequest(result, Defaults.NoPayload);
        }

        public _DEBUG_TYPED_DATA Evaluate(string toEvaluate)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_EVALUATE;
            result.Flags = 0;
            result.InStrIndex = (uint)Marshal.SizeOf(result);

            return PerformRequest(result, Encoding.Default.GetBytes(toEvaluate)).OutData;
        }

        public _DEBUG_TYPED_DATA GetArrayItem(_DEBUG_TYPED_DATA pointer, ulong index)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_GET_ARRAY_ELEMENT;
            result.InData = pointer;
            result.In64 = index;

            var response = PerformRequest(result, Defaults.NoPayload);
            return response.OutData;
        }

        public _DEBUG_TYPED_DATA Dereference(_DEBUG_TYPED_DATA typedData)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_GET_DEREFERENCE;
            result.InData = typedData;
            result.Status = 0;

            var response = PerformRequest(result, Defaults.NoPayload);
            return response.OutData;
        }

        public _DEBUG_TYPED_DATA GetField(_DEBUG_TYPED_DATA typedData, string field)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_GET_FIELD;
            result.InData = typedData;
            result.InStrIndex = (uint)Marshal.SizeOf(result);
            result.Status = 0;

            var response = PerformRequest(result, Encoding.Default.GetBytes(field));
            return response.OutData;
        }

        public _DEBUG_TYPED_DATA OutputTypeDefinition(_DEBUG_TYPED_DATA typedData)
        {
            var result = default(_EXT_TYPED_DATA);
            result.Operation = _EXT_TDOP.EXT_TDOP_OUTPUT_TYPE_DEFINITION;
            result.InData = typedData;
            result.Status = 0;

            var response = PerformRequest(result, Defaults.NoPayload);
            return response.OutData;
        }

        public byte[] ReadValue(ulong offset, uint size)
        {
            var buffer = new byte[size];
            uint actualBytes;
            var hr = _spaces.ReadVirtual(offset, buffer, size, out actualBytes);
            if (hr != HResult.Ok)
                return new byte[0];

            return buffer;
        }

        public string ReadString(ulong offset, uint size)
        {
            var buffer = new StringBuilder((int)size);
            uint actualBytes;
            var hr = _spaces.ReadUnicodeStringVirtual(offset, size, CODE_PAGE.UTF8, buffer, size, out actualBytes);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }

        public long ReadLong(_DEBUG_TYPED_DATA field)
        {
            switch (field.Size)
            {
                case 2:
                    return BitConverter.ToInt16(ReadValue(field.Offset, field.Size), 0);

                case 4:
                    return BitConverter.ToInt32(ReadValue(field.Offset, field.Size), 0);

                case 8:
                    return BitConverter.ToInt64(ReadValue(field.Offset, field.Size), 0);

                default:
                    throw new ArgumentException($"Vector size field has unsupported length '{field.Size}'");
            }
        }

        public string[] ReadFieldNames(_DEBUG_TYPED_DATA data)
        {
            List<string> result = new List<string>();
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint nameSize;
            uint fieldIndex = 0;
            var hr = _symbols.GetFieldNameWide(data.ModBase, data.TypeId, fieldIndex, buffer, Defaults.BufferSize, out nameSize);
            while (hr == HResult.Ok)
            {
                result.Add(buffer.ToString());
                fieldIndex++;
                hr = _symbols.GetFieldNameWide(data.ModBase, data.TypeId, fieldIndex, buffer, Defaults.BufferSize, out nameSize);
            }

            return result.ToArray();
        }

        public TypedVariable ReadVariable(_DEBUG_TYPED_DATA data)
        {
            _DEBUG_TYPED_DATA dereferenced = default(_DEBUG_TYPED_DATA);
            bool isDereferenced = false;
            if (data.Tag == (uint)SymTag.PointerType)
            {
                dereferenced = Dereference(data);
                isDereferenced = true;
            }

            var dataToOperate = isDereferenced ? dereferenced : data;
            var fieldNames = ReadFieldNames(dataToOperate);
            var fields = fieldNames.Select(x => new KeyValuePair<string, TypedVariable>(x, ReadVariable(GetField(dataToOperate, x))));
            var fieldsMap = new Dictionary<string, TypedVariable>();
            foreach (var pair in fields)
            {
                var key = GetKey(fieldsMap, pair.Key);
                fieldsMap.Add(key, pair.Value);
            }

            return new TypedVariable(data, dereferenced, fieldsMap);
        }

        private static string GetKey(Dictionary<string, TypedVariable> fieldsMap, string baseKey)
        {
            var key = baseKey;
            int counter = 1;
            while (fieldsMap.ContainsKey(key))
            {
                key = $"{baseKey}_{counter}";
                counter++;
            }

            return key;
        }

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
            _EXT_TYPED_DATA result = default(_EXT_TYPED_DATA);

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
    }
}
