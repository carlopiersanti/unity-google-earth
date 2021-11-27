

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Inking
{
    public class TextureLoader : MonoBehaviour
    {
#if UNITY_IOS && !UNITY_EDITOR
        public const string DllName = "__Internal";
#else
        public const string DllName = "async-texture-loader"; 
#endif

        [DllImport(DllName)]
        static extern IntPtr Inking_TextureLoader_GetInstance();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        [DllImport(DllName)]
        static extern IntPtr Inking_TextureLoader_LoadAsync(
            IntPtr _native,
            IntPtr data, int width, int height,
            IntPtr computeBufferIndices, IntPtr pinnedIndices, int indicesLength,
            IntPtr computeBufferVertex, IntPtr pinnedVertices, int verticesLength);
#else
        [DllImport(DllName)]
        static extern IntPtr Inking_TextureLoader_LoadAsync(
            IntPtr _native,
            IntPtr data, int width, int height,
            IntPtr computeBufferIndices, IntPtr pinnedIndices, int indicesLength,
            IntPtr computeBufferVertex, IntPtr pinnedVertices, int verticesLength);
#endif
        [DllImport(DllName)]
        static extern void Inking_TextureLoader_Update(IntPtr native);

        [DllImport(DllName)]
        static extern IntPtr GetRenderEventFunc();

        IntPtr _native;

        public TextureLoader()
        {
        }

        ~TextureLoader()
        {
        }

        static TextureLoader _instance;
        public static TextureLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject(typeof(TextureLoader).Name);
                    DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<TextureLoader>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            _native = Inking_TextureLoader_GetInstance();
        }

        public void Start()
        {
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
        }

        public TextureLoadAsyncOperation LoadAsync(
            IntPtr data, int width, int height,
            IntPtr computeBufferIndices, IntPtr pinnedIndices, int indicesLength,
            IntPtr computeBufferVertex, IntPtr pinnedVertices, int verticesLength)
        {
            IntPtr ptr = Inking_TextureLoader_LoadAsync(
                _native, data, width, height,
                computeBufferIndices, pinnedIndices, indicesLength,
                computeBufferVertex, pinnedVertices, verticesLength);
            var operation = new TextureLoadAsyncOperation(ptr, data, width, height,
                computeBufferIndices, pinnedIndices, indicesLength,
                computeBufferVertex, pinnedVertices, verticesLength);
            return operation;
        }

        public void Update()
        {
            UnityEngine.Profiling.Profiler.BeginSample("005");

            Inking_TextureLoader_Update(_native);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        IEnumerator _LoadAsync(
            IntPtr data, int width, int height,
            IntPtr computeBufferIndices, IntPtr pinnedIndices, int indicesLength,
            IntPtr computeBufferVertex, IntPtr pinnedVertices, int verticesLength,
            Action<Texture2D> onLoadSucceed, Action onLoadFailed)
        {
            var operation = LoadAsync(
                data, width, height,
                computeBufferIndices, pinnedIndices, indicesLength,
                computeBufferVertex, pinnedVertices, verticesLength);

            yield return operation;

            if (operation != null 
                && operation.state == TextureLoadAsyncOperationState.LoadSucceed)
            {
                if(onLoadSucceed != null)
                    onLoadSucceed.Invoke(operation.texture2D);
            }
            else
            {
                Debug.LogError(operation.state);
                if(onLoadFailed != null)
                    onLoadFailed.Invoke();
            }
        }

        public void LoadAsync(
            IntPtr data, int width, int height,
            IntPtr computeBufferIndices, IntPtr pinnedIndices, int indicesLength,
            IntPtr computeBufferVertex, IntPtr pinnedVertices, int verticesLength,
            Action<Texture2D> onLoadSucceed, Action onLoadFailed)
        {
            StartCoroutine(_LoadAsync(
                data, width, height,
                computeBufferIndices, pinnedIndices, indicesLength,
                computeBufferVertex, pinnedVertices, verticesLength,
                onLoadSucceed, onLoadFailed));
        }
    }
}
