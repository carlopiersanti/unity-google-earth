#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#if _MSC_VER // this is defined when compiling with Visual Studio
#define EXPORT_API __declspec(dllexport) // Visual Studio needs annotating exported functions with this
#else
#define EXPORT_API // XCode does not need annotating exported functions, so define is empty
#endif


extern "C" {
	EXPORT_API unsigned char* export_stbi_load_from_memory(stbi_uc const* buffer, int len, int* x, int* y, int* comp, int req_comp)
	{
		return stbi_load_from_memory(buffer, len, x, y, comp, req_comp);
	}

	EXPORT_API void export_stbi_image_free(void* retval_from_stbi_load)
	{
		stbi_image_free(retval_from_stbi_load);
	}
}