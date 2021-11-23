#pragma once

#include "ITextureLoader.h"
#include <d3d11.h>
#include <windows.h>
#include "TextureLoadAsyncOperation.h"
#include <mutex>
#include <list>
using namespace std;
namespace Inking
{
    class TextureLoaderD3D11 : public ITextureLoader
    {
        ID3D11Device* _device = NULL;
        ID3D11DeviceContext* _context = NULL;

        void Load(TextureLoadAsyncOperation* operation);

        void _AsyncLoadThreadFunc();

        thread _thread;

        list<TextureLoadAsyncOperation*> _stage1Operations;
        list<TextureLoadAsyncOperation*> _stage2Operations;

        mutex _stage1Mutex;
        mutex _stage2Mutex;
        bool isLoadThreadRuning = true;
    public:
        TextureLoaderD3D11();

        virtual ~TextureLoaderD3D11();

        static void AsyncLoadThreadFunc(TextureLoaderD3D11* _this);

        void LoadShared(TextureLoadAsyncOperation* operation);

        TextureLoadAsyncOperation* LoadAsync(
            byte* data, const int width, const int height,
            void* computeBufferIndices, byte* pinnedIndices, int indicesLength,
            void* computeBufferVertex, byte* pinnedVertices, int verticesLength);

        virtual void Update();

        virtual void Unload(void* nativeTex);

        virtual void OnUnityPluginLoad(IUnityInterfaces* unityInterfaces);

        virtual void OnUnityRenderingEvent(int eventID);

        virtual void OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType) {};

        virtual void OnUnityPluginUnload() {}
    };
}
