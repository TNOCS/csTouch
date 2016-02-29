// Serialization and code copyright (c) 2015 Steven de Jong, TNO.

using System.Collections.Generic;

namespace csCommon.Types.TextAnalysis.TextFinder.Trie
{
    /// <summary>
    /// Fast but potentially memory intensive dictionary in which lookup can be performed on partial keys (prefixes and infixes).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InfixTrie<T> : ITrie<T>
    {
        private readonly InfixTrieNode<T> _root;
        private readonly Dictionary<char, List<InfixTrieNode<T>>> _infixRoots;
        private readonly bool _supportInfix;

        /// <summary>
        /// Construct a trie.
        /// </summary>
        /// <param name="supportInfix">Whether to support infix searching, which is an order of magnitude slower (but still pretty fast).</param>
        public InfixTrie(bool supportInfix = false)
        {
            _root = new InfixTrieNode<T>(null);
            _infixRoots = supportInfix ? new Dictionary<char, List<InfixTrieNode<T>>>() : null;
            _supportInfix = supportInfix;
        }

        /// <summary>
        /// Add the data given under the key given.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="obj">The data to add</param>
        public void Add(string key, T obj)
        {
            var node = FindNode(_root, key, 0, true);
            node.AddContent(obj);
        }

        /// <summary>
        /// Retrieve all data that is stored under a key that contains the query string. Depending on
        /// whether the trie supports infixes, the query string matches a key if it is an infix or a 
        /// prefix of a key, or a prefix only.
        /// </summary>
        /// <param name="query">The query. Note that the trie is case sensitive.</param>
        /// <returns>All data for which the keys are matching the query.</returns>
        public IEnumerable<T> Retrieve(string query)
        {
            List<InfixTrieNode<T>> roots = new List<InfixTrieNode<T>>();
            if (_supportInfix)
            {
                List<InfixTrieNode<T>> allNodes = _root.GatherNodes();
                IEnumerable<InfixTrieNode<T>> leafs = FindLeafs(query, 0, allNodes);
                foreach (InfixTrieNode<T> infixTrieNode in leafs)
                {
                    InfixTrieNode<T> parent = infixTrieNode;
                    for (int i = 0; i < query.Length; i++)
                    {
                        parent = parent.Parent;
                    }
                    roots.Add(parent);
                }
            }
            else
            {
                roots.Add(_root);
            }

            List<T> ret = new List<T>();
            foreach (InfixTrieNode<T> root in roots)
            {
                var node = FindNode(root, query, 0, false);
                if (node != null)
                {
                    ret.AddRange(node.GatherContent());
                }
            }
            return ret;
        }

        private InfixTrieNode<T> FindNode(InfixTrieNode<T> root, string query, int position, bool createIfNeeded)
        {
            char key = query[position];
            if (position == query.Length - 1)
            {
                InfixTrieNode<T> node;
                if (root.TryGetChild(key, out node))
                {
                    return node;
                }
                else if (createIfNeeded)
                {
                    node = new InfixTrieNode<T>(root, key);
                    root.AddChild(key, node);
                    List<InfixTrieNode<T>> roots;
                    if (_supportInfix)
                    {
                        if (_infixRoots.TryGetValue(key, out roots))
                        {
                            roots.Add(node);
                        }
                        else
                        {
                            _infixRoots[key] = new List<InfixTrieNode<T>>() { node };
                        }                        
                    }
                    return node;
                }
            }
            else
            {
                InfixTrieNode<T> node;
                if (root.TryGetChild(key, out node))
                {
                    return FindNode(node, query, position + 1, createIfNeeded);
                }
                else if (createIfNeeded)
                {
                    node = new InfixTrieNode<T>(root, key);
                    root.AddChild(key, node);
                    List<InfixTrieNode<T>> roots;
                    if (_supportInfix)
                    {
                        if (_infixRoots.TryGetValue(key, out roots))
                        {
                            roots.Add(node);
                        }
                        else
                        {
                            _infixRoots[key] = new List<InfixTrieNode<T>>() { node };
                        }                        
                    }
                    return FindNode(node, query, position + 1, true);
                }
            }
            return null;
        }

        private IEnumerable<InfixTrieNode<T>> FindLeafs(string query, int position, IEnumerable<InfixTrieNode<T>> candidates)
        {
            if (position == query.Length)
            {
                return candidates;
            }

            char key = query[position];
            List<InfixTrieNode<T>> newCandidates = new List<InfixTrieNode<T>>();
            foreach (InfixTrieNode<T> infixTrieNode in candidates)
            {
                InfixTrieNode<T> value;
                if (infixTrieNode.TryGetChild(key, out value))
                {
                    newCandidates.Add(value);
                }
            }

            return FindLeafs(query, position + 1, newCandidates);
        }

        private class InfixTrieNode<T>
        {
            public InfixTrieNode(InfixTrieNode<T> parent, char? parentEdgeLabel = null)
            {
                Parent = parent;
                Content = new List<T>();
                Children = new Dictionary<char, InfixTrieNode<T>>();
                ParentEdgeLabel = parentEdgeLabel; // Some more memory, which may save e.g. autocomplete computation time.
            }

            public InfixTrieNode<T> Parent { get; private set; }
            private List<T> Content { get; set; }
            private Dictionary<char, InfixTrieNode<T>> Children { get; set; }
            public char? ParentEdgeLabel { get; private set; }

            public void AddContent(T content)
            {
                if (Content == null)
                {
                    Content = new List<T>();
                }
                Content.Add(content);
            }

            public void AddChild(char key, InfixTrieNode<T> child)
            {
                if (Children == null)
                {
                    Children = new Dictionary<char, InfixTrieNode<T>>();
                }
                Children[key] = child;
            }

            public bool TryGetChild(char key, out InfixTrieNode<T> result)
            {
                if (Children == null)
                {
                    result = null;
                    return false;
                }
                return Children.TryGetValue(key, out result);
            }

            public List<InfixTrieNode<T>> GatherNodes()
            {
                List<InfixTrieNode<T>> ret = new List<InfixTrieNode<T>>();
                GatherNodes(ret);
                return ret;
            }

            private void GatherNodes(List<InfixTrieNode<T>> into)
            {
                into.Add(this);
                foreach (var node in Children)
                {
                    node.Value.GatherNodes(into);
                }
            }

            public List<T> GatherContent()
            {
                List<T> ret = new List<T>();
                GatherContent(ret);
                return ret;
            }

            private void GatherContent(List<T> into)
            {
                into.AddRange(Content);
                foreach (var node in Children)
                {
                    node.Value.GatherContent(into);
                }
            }
        }
    }
}