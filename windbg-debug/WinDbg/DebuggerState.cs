using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg
{
    public class DebuggerState
    {
        #region Fields

        private int _indexCounter = Defaults.FirstIndex;
        private Dictionary<int, StackTraceFrame> _frames = new Dictionary<int, StackTraceFrame>();
        private Dictionary<int, Scope> _scopes = new Dictionary<int, Scope>();
        private Dictionary<int, IDebugSymbolGroup2> _symbolGroups = new Dictionary<int, IDebugSymbolGroup2>();
        private Dictionary<int, DebuggeeThread> _threads = new Dictionary<int, DebuggeeThread>();
        private Dictionary<int, Variable> _variables = new Dictionary<int, Variable>();
        private Dictionary<int, HashSet<int>> _children = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, VariableMetaData> _variableDescriptions = new Dictionary<int, VariableMetaData>();

        #endregion

        #region Public Properties

        public int CurrentThread { get; internal set; }
        public int CurrentFrame { get; internal set; }

        #endregion

        #region Public Methods

        public IEnumerable<DebuggeeThread> GetThreads()
        {
            return _threads.Values.ToList();
        }

        public IEnumerable<Scope> GetScopes(int frameId)
        {
            return GetChildren(frameId, _scopes);
        }

        public IEnumerable<StackTraceFrame> GetFrames(int threadId)
        {
            return GetChildren(threadId, _frames);
        }

        public IEnumerable<Variable> GetVariablesByScope(int scopeId)
        {
            return GetChildren(scopeId, _variables);
        }

        public IEnumerable<Variable> ExpandVariable(int variableId)
        {
            return GetChildren(variableId, _variables);
        }

        public void Clear()
        {
            _frames.Clear();
            _symbolGroups.Clear();
            _scopes.Clear();
            _variables.Clear();
            _threads.Clear();
            _children.Clear();
            CurrentFrame = Defaults.NoFrame;
            CurrentThread = Defaults.NoThread;
            _indexCounter = Defaults.FirstIndex;
        }

        public void AddThreads(IEnumerable<DebuggeeThread> threads)
        {
            if (threads == null)
                return;

            foreach (var item in threads)
            {
                _threads[item.Id] = item;
            }
        }

        public DebuggeeThread GetThread(int threadId)
        {
            return GetItem(threadId, _threads);
        }

        public StackTraceFrame AddFrame(int threadId, Func<int, StackTraceFrame> factory)
        {
            return AddItem(factory, _frames, threadId);
        }

        public StackTraceFrame GetFrame(int frameId)
        {
            return GetItem(frameId, _frames);
        }

        public Scope AddScope(int frameId, Func<int, Scope> factory)
        {
            return AddItem(factory, _scopes, frameId);
        }

        public Scope GetScope(int scopeId)
        {
            return GetItem(scopeId, _scopes);
        }

        public IDebugSymbolGroup2 GetSymbolsForScope(int scopeId)
        {
            IDebugSymbolGroup2 result;
            if (_symbolGroups.TryGetValue(scopeId, out result))
                return result;

            return null;
        }

        public void UpdateSymbolGroup(int id, IDebugSymbolGroup2 group)
        {
            _symbolGroups[id] = group;
        }

        public Variable AddVariable(int parentId, Func<int, Variable> factory, _DEBUG_TYPED_DATA entry)
        {
            var result = AddItem(factory, _variables, parentId);
            _variableDescriptions[result.Id] = new VariableMetaData(result.Name, result.Type, entry);

            return result;
        }

        public Variable GetVariable(int variableId)
        {
            return GetItem(variableId, _variables);
        }

        public DebuggeeThread GetThreadForFrame(int frameId)
        {
            var threadId = _children.FirstOrDefault(x => x.Value.Contains(frameId)).Key;
            return GetThread(threadId);
        }

        public Scope GetScopeForVariable(int variableId)
        {
            var scopeId = GetTopMostParentScope(variableId);
            return GetScope(scopeId);
        }

        public StackTraceFrame GetFrameForScope(int scopeId)
        {
            var frameId = _children.FirstOrDefault(x => x.Value.Contains(scopeId)).Key;
            return GetFrame(frameId);
        }

        public VariableMetaData GetVariableDescription(int variableId)
        {
            return GetItem(variableId, _variableDescriptions);
        }

        #endregion

        #region Private Methods

        private List<T> GetChildren<T>(int parentId, Dictionary<int, T> container)
        {
            HashSet<int> indices;
            if (!_children.TryGetValue(parentId, out indices))
                return new List<T>();

            List<T> result = new List<T>();
            foreach (var index in indices)
            {
                T value;
                if (container.TryGetValue(index, out value))
                    result.Add(value);
            }

            return result;
        }

        private void AddConnection(int key, int index)
        {
            HashSet<int> children;
            if (!_children.TryGetValue(key, out children))
            {
                children = new HashSet<int>();
                _children[key] = children;
            }

            children.Add(index);
        }

        private int GetNewIndex()
        {
            return Interlocked.Increment(ref _indexCounter);
        }

        private bool HasChild(int parentId, int childId)
        {
            HashSet<int> children;
            if (!_children.TryGetValue(parentId, out children))
                return false;

            return children.Contains(childId);
        }

        private int GetTopMostParentScope(int childId)
        {
            foreach (var item in _children)
            {
                if (item.Value.Contains(childId))
                {
                    if (_scopes.ContainsKey(item.Key))
                        return item.Key;

                    return GetTopMostParentScope(item.Key);
                }
            }

            return childId;
        }

        private T GetItem<T>(int itemId, Dictionary<int, T> container)
        {
            T result;
            if (container.TryGetValue(itemId, out result))
                return result;

            return default(T);
        }

        private T AddItem<T>(Func<int, T> factory, Dictionary<int, T> container, int parentId)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var index = GetNewIndex();
            AddConnection(parentId, index);

            var result = factory(index);
            container.Add(index, result);

            return result;
        }

        #endregion
    }
}
