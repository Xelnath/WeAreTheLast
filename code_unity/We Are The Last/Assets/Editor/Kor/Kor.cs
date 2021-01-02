using UnityEngine;
using UnityEditor;
using DryIoc;
using System;
using System.Collections.Generic;
using Atomech;

namespace Kor
{
    public interface IKorPlugin
    {
        void Bind(IContainer _);
        int Priority { get; }
        bool Enabled { get; }
    }

    // Sini recommended this helper function
    public static class Gimme<T>
    {
        public static T Now()
        {
            return Kor.Instance.Container.Resolve<T>();
        }
        public static T Now(IResolverContext scope)
        {
            return scope.Resolve<T>();
        }
    }


    public sealed class Kor
    {
        private static Kor m_instance;
        private Container m_container;

        [InitializeOnLoadMethod]
        public static void InitializeKor()
        {
            if (m_instance != null)
                return;
            try
            {
                m_instance = new Kor();
                m_instance.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to initialize Kor");
                Debug.LogException(ex);
                m_instance = null;
            }
        }

        public static bool IsReady
        {
            get { return m_instance != null; }
        }

        public static Kor Instance
        {
            get {
                if (m_instance == null)
                {
                    throw new Exception("You can't access the Kor instance yet. It hasn't been initialized!");
                }
                return m_instance;
            }
        }

        public Container Container
        {
            get {
                if (m_container == null)
                {
                    throw new Exception("Container is not initialized. Check the console for exceptions.");
                }
                return m_container;
            }
        }

        public const string AssembliesPrefix = "__";

        public void Initialize()
        {
            m_container = new Container();
            try
            {
                ServePlugins();

                // TODO : Consider making this a core feature or not?
                var initializableTypes = new List<Type>(10);
                var initializable = new List<IInitializable>(10);

                ReflectionHelper.GetTypesImplementing<IInitializable>(initializableTypes);
                ReflectionHelper.InstantiateAllAs(initializableTypes, initializable);
                initializable.ForEach(t => {
                    Container.RegisterDelegate(typeof(IInitializable), ctx => t, Reuse.Singleton);
                    Container.RegisterDelegate(t.GetType(), ctx => t, Reuse.Singleton);
                });

                IEnumerable<IInitializable> inits = Container.Resolve<IEnumerable<IInitializable>>();
                InitializeAll(inits);

            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to serve Kor plugins.");
                Debug.LogException(ex);
            }
        }

        private Rules ConfigureMainContainer(Rules rules)
        {
            return rules.WithResolveIEnumerableAsLazyEnumerable();
        }

        public void ServePlugins()
        {
            var pluginTypes = new List<Type>(100);
            var plugins = new List<IKorPlugin>(100);

            ReflectionHelper.GetTypesImplementing<IKorPlugin>(pluginTypes);
            ReflectionHelper.InstantiateAllAs(pluginTypes, plugins);
            plugins.Sort((plugin1, plugin2) => plugin1.Priority.CompareTo(plugin2.Priority));
            plugins.ForEach(plugin => { m_container.RegisterDelegate(ctx => plugins, Reuse.Singleton); });
            plugins.ForEach(Bind);

        }

        public void Bind(IKorPlugin plugin)
        {
            if (!plugin.Enabled)
                return;

            try
            {
                plugin.Bind(Container);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to bind plugin " + plugin.GetType().Name);
                Debug.LogException(ex);
            }
        }

        public void InitializeAll(IEnumerable<IInitializable> initializables)
        {
            // Grab all initializables and initialize them
            foreach (var initializable in initializables)
            {
                try
                {
                    initializable.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }


    }
}