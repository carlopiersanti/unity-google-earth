#pragma once
#include "RefCounter.h"

namespace Inking
{
    class ITextureLoader;

    class Texture : public RefCounter
    {
    public:
        void* _native = nullptr;
        void* _nativeIndices = nullptr;
        void* _nativeVertexes = nullptr;
        ITextureLoader* _textureLoader = nullptr;
        void* _query = nullptr;

    public:
        Texture(ITextureLoader* textureLoader);

        virtual ~Texture();

        void* GetNative()
        {
            return _native;
        }

        void SetNative(void* value)
        {
            _native = value;
        }

        void SetNativeVertices(void* value)
        {
            _nativeIndices = value;
        }

        void SetNativeIndexes(void* value)
        {
            _nativeVertexes = value;
        }

        void SetQuery(void* value)
        {
            _query = value;
        }
    };
}
