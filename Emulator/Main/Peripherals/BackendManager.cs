//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using Emul8.Utilities;
using System.Linq;
using Antmicro.Migrant;
using Antmicro.Migrant.Hooks;
using Emul8.Logging;
using Emul8.Utilities.Collections;

namespace Emul8.Peripherals
{
    public class BackendManager
    {
        public BackendManager()
        {
            map = new SerializableWeakKeyDictionary<IAnalyzable, IAnalyzableBackend>();
            Init();
        }

        public IEnumerable<string> GetAvailableAnalyzersFor(IAnalyzableBackend backend)
        {
            if (!analyzers.ContainsKey(backend.GetType()))
            {
                return new string[0];
            }

            return analyzers[backend.GetType()].Select(x => (IAnalyzableBackendAnalyzer)Activator.CreateInstance(x)).Select(y => y.Id);
        }

        public void SetPreferredAnalyzer(Type backendType, Type analyzerType)
        {
            preferredAnalyzer[backendType] = analyzerType;
        }

        public string GetPreferredAnalyzerFor(IAnalyzableBackend backend)
        {
            return preferredAnalyzer.ContainsKey(backend.GetType()) ? ((IAnalyzableBackendAnalyzer)Activator.CreateInstance(preferredAnalyzer[backend.GetType()])).Id : null;
        }

        public bool TryCreateBackend<T>(T analyzable) where T : IAnalyzable
        {
            Type backendType = null;
            foreach(var b in backends)
            {
                if(b.Key.IsAssignableFrom(analyzable.GetType()))
                {
                    backendType = b.Value;
                    break;
                }
            }

            if(backendType != null)
            {
                dynamic backend = (IAnalyzableBackend) Activator.CreateInstance(backendType);
                backend.Attach((dynamic)analyzable);
                map[analyzable] = backend;

                return true;
            }
            return false;
        }

        public bool TryGetBackendFor(IAnalyzable peripheral, out IAnalyzableBackend backend)
        {
            return map.TryGetValue(peripheral, out backend);
        }

        public bool TryGetBackendFor<T>(T element, out IAnalyzableBackend<T> backend) where T : IAnalyzable
        {
            IAnalyzableBackend outValue = null;
            var result = map.TryGetValue(element, out outValue);
            backend = (IAnalyzableBackend<T>)outValue;
            return result;
        }

        public bool TryCreateAnalyzerForBackend<T>(T backend, out IAnalyzableBackendAnalyzer analyzer) where T : IAnalyzableBackend
        {
            if (!analyzers.ContainsKey(backend.GetType()))
            {
                analyzer = null;
                return false;
            }
            var foundAnalyzers = analyzers[backend.GetType()];

            Type analyzerType;
            if(foundAnalyzers.Count > 1)
            {
                if(preferredAnalyzer.ContainsKey(backend.GetType()))
                {
                    analyzerType = preferredAnalyzer[backend.GetType()];
                }
                else
                {
                    analyzer = null;
                    return false;
                }
            }
            else
            {
                analyzerType = foundAnalyzers.First();
            }

            analyzer = CreateAndAttach(analyzerType, backend);
            activeAnalyzers.Add(analyzer);
            return true;
        }

        public bool TryCreateAnalyzerForBackend<T>(T backend, string id, out IAnalyzableBackendAnalyzer analyzer) where T : IAnalyzableBackend
        {
            if (!analyzers.ContainsKey(backend.GetType()))
            {
                analyzer = null;
                return false;
            }
            var foundAnalyzers = analyzers[backend.GetType()];
            foreach(var found in foundAnalyzers)
            {
                if(TryCreateAndAttach(found, backend, a => a.Id == id, out analyzer))
                {
                    activeAnalyzers.Add(analyzer);
                    return true;
                }
            }

            analyzer = null;
            return false;
        }

        public void HideAnalyzersFor(IPeripheral peripheral)
        {
            var toRemove = new List<IAnalyzableBackendAnalyzer>();
            foreach(var analyzer in activeAnalyzers.Where(x => x.Backend.AnalyzableElement == peripheral))
            {
                analyzer.Hide();
                toRemove.Add(analyzer);
            }

            foreach(var rem in toRemove)
            {
                activeAnalyzers.Remove(rem);
            }
        }

        private IAnalyzableBackendAnalyzer CreateAndAttach(Type analyzerType, object backend)
        {
            dynamic danalyzer = Activator.CreateInstance(analyzerType);
            danalyzer.AttachTo((dynamic)backend);
            return (IAnalyzableBackendAnalyzer) danalyzer;
        }

        private bool TryCreateAndAttach(Type analyzerType, object backend, Func<IAnalyzableBackendAnalyzer, bool> condition, out IAnalyzableBackendAnalyzer analyzer)
        {
            dynamic danalyzer = Activator.CreateInstance(analyzerType);
            if(condition(danalyzer))
            {
                danalyzer.AttachTo((dynamic)backend);
                analyzer = (IAnalyzableBackendAnalyzer)danalyzer;
                return true;
            }

            analyzer = null;
            return false;
        }

        private void HandleAutoLoadTypeFound(Type t)
        {
            var interestingInterfaces = t.GetInterfaces().Where(i => i.IsGenericType && 
                (i.GetGenericTypeDefinition() == typeof(IAnalyzableBackendAnalyzer<>) ||
                    i.GetGenericTypeDefinition() == typeof(IAnalyzableBackend<>)));

            if(!interestingInterfaces.Any())
            {
                return;
            }

            var analyzerTypes = interestingInterfaces.Where(i => i.GetGenericTypeDefinition() == typeof(IAnalyzableBackendAnalyzer<>)).SelectMany(i => i.GetGenericArguments()).ToArray();
            foreach(var arg in analyzerTypes)
            {
                if(!analyzers.ContainsKey(arg))
                {
                    analyzers.Add(arg, new List<Type>());
                }

                analyzers[arg].Add(t);
            }

            var backendTypes = interestingInterfaces.Where(i => i.GetGenericTypeDefinition() == typeof(IAnalyzableBackend<>)).SelectMany(i => i.GetGenericArguments()).ToArray();
            foreach(var arg in backendTypes)
            {
                if(backends.ContainsKey(arg))
                {
                    throw new InvalidProgramException(string.Format("There can be only one backend class for a peripheral type, but found at least two: {0}, {1}", backends[arg].AssemblyQualifiedName, t.AssemblyQualifiedName));
                }
                backends[arg] = t;
            }
        }

        [PreSerialization]
        private void SavePreferredAnalyzers()
        {
            preferredAnalyzersString = new Dictionary<string, string>();
            foreach(var pa in preferredAnalyzer)
            {
                preferredAnalyzersString.Add(pa.Key.AssemblyQualifiedName, pa.Value.AssemblyQualifiedName);
            }
        }

        private void RestorePreferredAnalyzers()
        {
            if(preferredAnalyzersString == null)
            {
                return;
            }

            foreach(var pas in preferredAnalyzersString)
            {
                try 
                {
                    preferredAnalyzer.Add(Type.GetType(pas.Key), Type.GetType(pas.Value));
                } 
                catch (Exception)
                {
                    Logger.LogAs(this, LogLevel.Warning, "Could not restore preferred analyzer for {0}: {1}. Error while loading types", pas.Key, pas.Value);
                }
            }

            preferredAnalyzersString = null;
        }

        [PostDeserialization]
        private void Init()
        {
            analyzers = new Dictionary<Type, List<Type>>();
            backends = new Dictionary<Type, Type>();
            preferredAnalyzer = new Dictionary<Type, Type>();
            activeAnalyzers = new List<IAnalyzableBackendAnalyzer>();

            RestorePreferredAnalyzers();
            TypeManager.Instance.AutoLoadedType += HandleAutoLoadTypeFound;
        }

        private SerializableWeakKeyDictionary<IAnalyzable, IAnalyzableBackend> map;
        [Transient]
        private Dictionary<Type, List<Type>> analyzers;
        [Transient]
        private Dictionary<Type, Type> backends;
        [Transient]
        private Dictionary<Type, Type> preferredAnalyzer;

        private Dictionary<string, string> preferredAnalyzersString;

        [Transient]
        private List<IAnalyzableBackendAnalyzer> activeAnalyzers;
    }
}

