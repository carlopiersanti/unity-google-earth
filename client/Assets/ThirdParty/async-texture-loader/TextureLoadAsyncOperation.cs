

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Inking
{
    public class TextureLoadAsyncOperation : CustomYieldInstruction
    {
        IntPtr data;
            int width; int height;
        IntPtr computeBufferIndices; IntPtr pinnedIndices; int indicesLength;
        IntPtr computeBufferVertex; IntPtr pinnedVertices; int verticesLength;

        [DllImport(TextureLoader.DllName)]
        static extern IntPtr Inking_TextureLoadAsyncOperation_New();

        [DllImport(TextureLoader.DllName)]
        static extern IntPtr Inking_TextureLoadAsyncOperation_Release(IntPtr native);

        [DllImport(TextureLoader.DllName)]
        static extern int Inking_TextureLoadAsyncOperation_GetState(IntPtr native);

        [DllImport(TextureLoader.DllName)]
        static extern IntPtr Inking_TextureLoadAsyncOperation_GetTexture(IntPtr native);

        IntPtr _native;

        public TextureLoadAsyncOperation()
        {
            _native = Inking_TextureLoadAsyncOperation_New();
        }

        public TextureLoadAsyncOperation(IntPtr native, IntPtr data, int width, int height,
            IntPtr computeBufferIndices, IntPtr pinnedIndices, int indicesLength,
            IntPtr computeBufferVertex, IntPtr pinnedVertices, int verticesLength)
        {
            _native = native;
            this.data = data;
            this.width = width;
            this.height = height;
            this.computeBufferIndices = computeBufferIndices;
            this.pinnedIndices = pinnedIndices;
            this.indicesLength = indicesLength;
            this.computeBufferVertex = computeBufferVertex;
            this.pinnedVertices = pinnedVertices;
            this.verticesLength = verticesLength;
        }

        ~TextureLoadAsyncOperation()
        {
            Inking_TextureLoadAsyncOperation_Release(_native);
        }

        public TextureLoadAsyncOperationState state
        {
            get
            {
                return (TextureLoadAsyncOperationState)Inking_TextureLoadAsyncOperation_GetState(_native);
            }
        }

        public override bool keepWaiting
        {
            get
            {
                return state == TextureLoadAsyncOperationState.None
                    || state == TextureLoadAsyncOperationState.Loading;
            }
        }

        public string fileName;

        Texture2D _texture2D;

        public Texture2D texture2D
        {
            get
            {
                if (_texture2D == null)
                {
                    IntPtr nativeTex = Inking_TextureLoadAsyncOperation_GetTexture(_native);
                    _texture2D = new Inking.Texture2D(nativeTex);
                }
                return _texture2D;
            }
        }
    }
}
