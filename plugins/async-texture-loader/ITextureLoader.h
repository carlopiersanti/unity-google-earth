#pragma once
#include <windows.h>
#include <thread>
using namespace std;
#include "RefCounter.h"
#include "../unity-api/IUnityGraphics.h"

struct IUnityInterfaces;

namespace Inking
{
    class TextureLoadAsyncOperation;

    class ITextureLoader : public RefCounter
    {
    public:
        virtual TextureLoadAsyncOperation* LoadAsync(
            byte* data, const int width, const int height,
            void* computeBufferIndices, byte* pinnedIndices, int indicesLength,
            void* computeBufferVertex, byte* pinnedVertices, int verticesLength) = 0;

        virtual void Update() = 0;

        virtual void Unload(void* nativeTex) = 0;

        virtual void OnUnityPluginLoad(IUnityInterfaces* unityInterfaces) = 0;

        virtual void OnUnityRenderingEvent(int eventID) = 0;

        virtual void OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType) = 0;

        virtual void OnUnityPluginUnload() = 0;
    };
}
