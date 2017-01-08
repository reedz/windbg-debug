using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg
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

        public StackTraceFrame AddFrame(int threadId, Func<int, StackTraceFrame> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var index = GetNewIndex();
            AddConnection(threadId, index);
            
            var result = factory(index);
            _frames.Add(index, result);

            return result;
        }

        public StackTraceFrame GetFrame(int frameId)
        {
            StackTraceFrame result;
            if (_frames.TryGetValue(frameId, out result))
                return result;

            return null;
        }

        public Scope AddScope(int frameId, Func<int, Scope> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var index = GetNewIndex();
            AddConnection(frameId, index);

            var result = factory(index);
            _scopes.Add(index, result);

            return result;
        }

        public Scope GetScope(int scopeId)
        {
            Scope result;
            if (_scopes.TryGetValue(scopeId, out result))
                return result;

            return null;
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

        public Variable AddVariable(int parentId, Func<int, Variable> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var index = GetNewIndex();
            AddConnection(parentId, index);

            var result = factory(index);
            _variables.Add(index, result);

            return result;
        }

        public Variable GetVariable(int variableId)
        {
            Variable result;
            if (_variables.TryGetValue(variableId, out result))
                return result;

            return null;
        }

        public Scope GetScopeForVariable(int variableId)
        {
            var scopeId = GetTopMostParentScope(variableId);
            return GetScope(scopeId);
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

        #endregion
    }
}
