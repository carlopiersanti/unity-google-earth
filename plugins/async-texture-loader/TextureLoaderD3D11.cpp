#include "TextureLoaderD3D11.h"
#include "Texture2D.h"

#define STBI_WINDOWS_UTF8
#include "../stb/stb_image.h"
#include "../unity-api/IUnityGraphicsD3D11.h"

#include<vector>

namespace Inking
{

    TextureLoaderD3D11::TextureLoaderD3D11()
    {

    }

    TextureLoaderD3D11::~TextureLoaderD3D11()
    {
        isLoadThreadRuning = false;
        _thread.join();
    }

    void TextureLoaderD3D11::AsyncLoadThreadFunc(TextureLoaderD3D11* _this)
    {
        _this->_AsyncLoadThreadFunc();
    }

    void TextureLoaderD3D11::LoadShared(TextureLoadAsyncOperation* operation)
    {
        operation->SetState(TextureLoadAsyncOperationState::Loading);

        D3D11_BUFFER_DESC descIndices;
        descIndices.ByteWidth = operation->indicesLength;
        descIndices.Usage = D3D11_USAGE_DYNAMIC;
        descIndices.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        descIndices.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        descIndices.MiscFlags = 0;
        descIndices.StructureByteStride = sizeof(int);

        D3D11_SUBRESOURCE_DATA dataIndices;
        dataIndices.pSysMem = operation->pinnedIndices;

        ID3D11Buffer* d3d11BufferIndices = nullptr;

        HRESULT hr = _device->CreateBuffer(&descIndices, &dataIndices, &d3d11BufferIndices);

        if (FAILED(hr))
        {
            operation->SetState(TextureLoadAsyncOperationState::LoadFailed);
            return;
        }







        D3D11_BUFFER_DESC descVertices;
        descVertices.ByteWidth = operation->verticesLength;
        descVertices.Usage = D3D11_USAGE_DYNAMIC;
        descVertices.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        descVertices.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;
        descVertices.MiscFlags = 0;
        descVertices.StructureByteStride = 3 * sizeof(float) + 2 * sizeof(float) + 2 * sizeof(float);

        D3D11_SUBRESOURCE_DATA dataVertices;
        dataVertices.pSysMem = operation->pinnedVertices;

        ID3D11Buffer* d3d11BufferVertices = nullptr;

        hr = _device->CreateBuffer(&descVertices, &dataVertices, &d3d11BufferVertices);

        if (FAILED(hr))
        {
            operation->SetState(TextureLoadAsyncOperationState::LoadFailed);
            return;
        }








        ID3D11Query* query;


        D3D11_QUERY_DESC descQuery;
        descQuery.MiscFlags = 0;
        descQuery.Query = D3D11_QUERY_EVENT;


        hr = _device->CreateQuery(&descQuery, &query);

        if (FAILED(hr))
        {
            operation->SetState(TextureLoadAsyncOperationState::_003);
            return;
        }




















        byte* dataRaw = operation->GetData();

        ID3D11Texture2D* d3d11Texture2D = nullptr;

        int width = operation->GetWidth();
        int height = operation->GetHeight();

        auto pixels = dataRaw;

        D3D11_TEXTURE2D_DESC desc;
        ZeroMemory(&desc, sizeof(desc));
        desc.Width = width;
        desc.Height = height;
        desc.MipLevels = 1;
        desc.ArraySize = 1;
        desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        desc.SampleDesc.Count = 1;
        desc.SampleDesc.Quality = 0;
        desc.Usage = D3D11_USAGE_DEFAULT;
        desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        desc.CPUAccessFlags = 0;
        desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;

        D3D11_SUBRESOURCE_DATA data = {};
        data.pSysMem = pixels;
        data.SysMemPitch = width * 4;
        data.SysMemSlicePitch = 1;

        hr = _device->CreateTexture2D(&desc, &data, &d3d11Texture2D);

        if (FAILED(hr))
        {
            operation->OnLoadFailed();
            return;
        }

        IDXGIResource* pResource = nullptr;
        hr = d3d11Texture2D->QueryInterface(__uuidof(IDXGIResource), reinterpret_cast<void**>(&pResource));
        if (FAILED(hr)) {
            operation->OnLoadFailed();
            return;
        }

        HANDLE textureSharedHandle = INVALID_HANDLE_VALUE;
        if (SUCCEEDED(hr))
        {
            pResource->GetSharedHandle(&textureSharedHandle);

            Texture2D* texture2D = new Texture2D(this);
            texture2D->SetNative(textureSharedHandle);
            texture2D->SetNativeVertices(d3d11BufferIndices);
            texture2D->SetNativeIndexes(d3d11BufferVertices);
            texture2D->SetQuery(query);
            texture2D->SetWidth(width);
            texture2D->SetHeight(height);

            operation->SetTexture(texture2D);
        }

        d3d11Texture2D->Release();
    }

    void TextureLoaderD3D11::Load(TextureLoadAsyncOperation* operation)
    {
        LoadShared(operation);

        _stage2Mutex.lock();
        _stage2Operations.push_front(operation);
        _stage2Mutex.unlock();
    }

    void TextureLoaderD3D11::_AsyncLoadThreadFunc()
    {
        while (isLoadThreadRuning)
        {
            Sleep(0);

            _stage1Mutex.lock();

            auto size = this->_stage1Operations.size();

            if (size == 0)
            {
                _stage1Mutex.unlock();
                continue;
            }

            auto operation = this->_stage1Operations.front();
            this->_stage1Operations.pop_front();
            _stage1Mutex.unlock();

            Load(operation);

        }
    }

    TextureLoadAsyncOperation* TextureLoaderD3D11::LoadAsync(
        byte* data, const int width, const int height,
        void* computeBufferIndices, byte* pinnedIndices, int indicesLength,
        void* computeBufferVertex, byte* pinnedVertices, int verticesLength)
    {
        auto operation = new TextureLoadAsyncOperation();
        _stage1Mutex.lock();
        _stage1Operations.push_back(operation);
        operation->SetData(
            data, width, height,
            computeBufferIndices, pinnedIndices, indicesLength,
            computeBufferVertex, pinnedVertices, verticesLength);
        _stage1Mutex.unlock();

        return operation;
    }

    void TextureLoaderD3D11::Update()
    {
        _stage2Mutex.lock();

        std::vector<TextureLoadAsyncOperation*> operationsToRemove;
        if (_stage2Operations.size() != 0)
        {
            for (auto operation : _stage2Operations)
            {
                if (operation->GetState() >= TextureLoadAsyncOperationState::LoadFailed)
                {
                    operationsToRemove.push_back(operation);
                    operation->SetTexture(nullptr);
                    continue;
                }

                Texture2D* texture2D = (Texture2D*)operation->GetTexture();

                if (!operation->bufferUploadOngoing)
                {
                    operation->bufferUploadOngoing = true;

                    _context->CopyResource((ID3D11Resource*)operation->computeBufferIndices, (ID3D11Resource*)texture2D->_nativeIndices);
                    _context->CopyResource((ID3D11Resource*)operation->computeBufferVertex, (ID3D11Resource*)texture2D->_nativeVertexes);
                    _context->Flush();

                    _context->End((ID3D11Asynchronous*)texture2D->_query);
                    continue;
                }
                else
                {

                    HRESULT hr2 = _context->GetData((ID3D11Asynchronous*)texture2D->_query, nullptr, 0, 0);

                    if (FAILED(hr2))
                        continue;

                    ((ID3D11Resource*)texture2D->_nativeIndices)->Release();
                    ((ID3D11Resource*)texture2D->_nativeVertexes)->Release();
                    ((ID3D11Asynchronous*)texture2D->_query)->Release();
                }

                operationsToRemove.push_back(operation);

                HANDLE handle = (HANDLE)texture2D->GetNative();
                if (handle != INVALID_HANDLE_VALUE)
                {
                    ID3D11Texture2D* d3d11Texture2D = nullptr;
                    ID3D11ShaderResourceView* srv = nullptr;
                    
                    HRESULT hr = _device->OpenSharedResource(handle, __uuidof(ID3D11Texture2D), (void**)&d3d11Texture2D);
                    if (SUCCEEDED(hr))
                    {
                        hr = _device->CreateShaderResourceView(d3d11Texture2D, NULL, &srv);
                        if (SUCCEEDED(hr))
                        {
                            texture2D->SetNative(srv);
                            operation->SetState(TextureLoadAsyncOperationState::LoadSucceed);
                            d3d11Texture2D->Release();
                            continue;
                        }
                    }

                    operation->SetTexture(nullptr);
                    operation->SetState(TextureLoadAsyncOperationState::LoadFailed);
                }
            }

            for (auto var : operationsToRemove)
            {
                _stage2Operations.remove(var);
            }
        }

        _stage2Mutex.unlock();
    }

    void TextureLoaderD3D11::Unload(void* nativeTex)
    {
        ID3D11ShaderResourceView * srv = (ID3D11ShaderResourceView *)nativeTex;
        if(srv)
            srv->Release();
    }

    void TextureLoaderD3D11::OnUnityPluginLoad(IUnityInterfaces* unityInterfaces)
    {
        IUnityGraphics* graphics = unityInterfaces->Get<IUnityGraphics>();
        auto renderer = graphics->GetRenderer();
        switch (renderer)
        {
        case kUnityGfxRendererD3D11:
        {
            auto graphicsD3D11 = unityInterfaces->Get<IUnityGraphicsD3D11>();
            _device = graphicsD3D11->GetDevice();
            _device->GetImmediateContext(&_context);
            _thread = thread(AsyncLoadThreadFunc, this);
            break;
        }
        default:
            break;
        }
    }

    void TextureLoaderD3D11::OnUnityRenderingEvent(int eventID)
    {

    }
}
