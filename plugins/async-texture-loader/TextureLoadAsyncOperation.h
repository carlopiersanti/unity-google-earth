#pragma once
#include "RefCounter.h"
#include "Texture.h"
#include "TextureLoadAsyncOpeartionState.h"
namespace Inking
{

    class TextureLoadAsyncOperation : public RefCounter
    {
    public:
        byte* data;
        int width;
        int height;
        void* computeBufferIndices;
        byte* pinnedIndices;
        int indicesLength;
        void* computeBufferVertex;
        byte* pinnedVertices;
        int verticesLength;

        Texture* _texture = nullptr;
        int nbCycles = 0;
        TextureLoadAsyncOperationState _state = TextureLoadAsyncOperationState::None;

        bool bufferUploadOngoing = false;

        TextureLoadAsyncOperation()
        {

        }

        TextureLoadAsyncOperationState GetState()
        {
            return _state;
        }

        void SetState(TextureLoadAsyncOperationState state)
        {
            _state = state;
        }

        Texture* GetTexture() const
        {
            return _texture;
        }

        void SetTexture(Texture* value)
        {
            _texture = value;
        }

        byte* GetData() {
            return data;
        }

        const int GetWidth() {
            return width;
        }
        const int GetHeight() {
            return height;
        }
        void SetData(
            byte* data, const int width, const int height,
            void* computeBufferIndices, byte* pinnedIndices, int indicesLength,
            void* computeBufferVertex, byte* pinnedVertices, int verticesLength) {
            this->data = data;
            this->width = width;
            this->height = height;
            this->computeBufferIndices = computeBufferIndices;
            this->pinnedIndices = pinnedIndices;
            this->indicesLength = indicesLength;
            this->computeBufferVertex = computeBufferVertex;
            this->pinnedVertices = pinnedVertices;
            this->verticesLength = verticesLength;
        }

        void OnLoadFailed() {
            SetState(TextureLoadAsyncOperationState::LoadFailed);
            SetTexture(nullptr);
        }
    };
}
