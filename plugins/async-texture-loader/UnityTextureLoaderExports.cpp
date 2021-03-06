#include "UnityTextureLoaderExports.h"
#include "UnityTextureLoader.h"
#include "Texture2D.h"
#include "TextureLoadAsyncOperation.h"

#define STB_IMAGE_IMPLEMENTATION
#define STBI_WINDOWS_UTF8
#include "../stb/stb_image.h"

extern "C"
{
    int Inking_Texture2D_GetWidth(Inking::Texture2D* _this)
    {
        if (_this == nullptr)
            return -1;

        return _this->GetWidth();
    }

    int Inking_Texture2D_GetHeight(Inking::Texture2D* _this)
    {
        if (_this == nullptr)
            return -1;

        return _this->GetHeight();
    }

    void Inking_Texture2D_Release(Inking::Texture2D* _this)
    {
        if (_this != nullptr)
        {
            _this->Release();
        }
    }

    void* Inking_Texture2D_GetNative(Inking::Texture2D* _this)
    {
        if (_this == nullptr)
            return nullptr;

        return _this->GetNative();
    }

    Inking::TextureLoadAsyncOperation* Inking_TextureLoadAsyncOperation_New() {
        return new Inking::TextureLoadAsyncOperation();
    }

    void Inking_TextureLoadAsyncOperation_Release(Inking::TextureLoadAsyncOperation* _this) 
    {
        if (_this)
            _this->Release();
    }

    int Inking_TextureLoadAsyncOperation_GetState(Inking::TextureLoadAsyncOperation* _this)
    {
        if (_this == nullptr)
            return -1;

        return (int)_this->GetState();
    }

    const Inking::Texture* Inking_TextureLoadAsyncOperation_GetTexture(Inking::TextureLoadAsyncOperation* _this)
    {
        if (_this == nullptr)
            return nullptr;

        return _this->GetTexture();
    }

    Inking::UnityTextureLoader* Inking_TextureLoader_GetInstance()
    {
        return Inking::UnityTextureLoader::GetInstance();
    }

    Inking::TextureLoadAsyncOperation* Inking_TextureLoader_LoadAsync(
        Inking::UnityTextureLoader* _this,
        byte* data, const int width, const int height,
        void* computeBufferIndices, byte* pinnedIndices, int indicesLength,
        void* computeBufferVertex, byte* pinnedVertices, int verticesLength)
    {
        if (_this == nullptr)
            return nullptr;

        return _this->LoadAsync(data, width, height, computeBufferIndices, pinnedIndices, indicesLength,
            computeBufferVertex, pinnedVertices, verticesLength);
    }

    void Inking_TextureLoader_Update(Inking::UnityTextureLoader* _this)
    {
        if (_this)
            _this->Update();
    }

    void Inking_TextureLoader_Unload(Inking::UnityTextureLoader* _this, void* native)
    {
        if (_this)
            _this->Unload(native);
    }
}




