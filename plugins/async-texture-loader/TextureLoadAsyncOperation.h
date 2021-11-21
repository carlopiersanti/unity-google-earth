#pragma once
#include "RefCounter.h"
#include "Texture.h"
#include "TextureLoadAsyncOpeartionState.h"
namespace Inking
{

    class TextureLoadAsyncOperation : public RefCounter
    {
        byte* data;
        int width;
        int height;

        Texture* _texture = nullptr;

        TextureLoadAsyncOperationState _state = TextureLoadAsyncOperationState::None;
    public:
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
        void SetData(byte* data, const int width, const int height) {
            this->data = data;
            this->width = width;
            this->height = height;
        }

        void OnLoadFailed() {
            SetState(TextureLoadAsyncOperationState::LoadFailed);
            SetTexture(nullptr);
        }
    };
}
