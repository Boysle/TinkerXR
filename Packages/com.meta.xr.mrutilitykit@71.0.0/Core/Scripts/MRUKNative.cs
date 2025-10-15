/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    internal static class MRUKNative
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        [DllImport("libdl.dylib")]
        public static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.dylib")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.dylib")]
        public static extern int dlclose(IntPtr handle);
#elif UNITY_ANDROID
        [DllImport("libdl.so")]
        public static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        public static extern int dlclose(IntPtr handle);
#else
#error Unsupported platform
#endif
        private static IntPtr _nativeLibraryPtr;

        // Cross-platform abstraction for loading a DLL or shared object
        private static IntPtr GetDllHandle(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return LoadLibrary(path);
#else
            const int RTLD_NOW = 2;
            return dlopen(path, RTLD_NOW);
#endif
        }

        // Cross-platform abstraction for accessing a symbol within a DLL or shared object
        private static IntPtr GetDllExport(IntPtr dllHandle, string name)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return GetProcAddress(dllHandle, name);
#else
            return dlsym(dllHandle, name);
#endif
        }

        // Cross-platform abstraction for freeing/closing a DLL or shared object
        private static bool FreeDllHandle(IntPtr dllHandle)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return FreeLibrary(dllHandle);
#else
            return dlclose(_nativeLibraryPtr) == 0;
#endif
        }

        internal static void LoadMRUKSharedLibrary()
        {
            if (_nativeLibraryPtr != IntPtr.Zero)
            {
                return;
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var path = Path.GetFullPath("Packages/com.meta.xr.mrutilitykit/Plugins/Win64/mrutilitykitshared.dll");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            string folder = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "MacArm" : "Mac";
            var path = Path.GetFullPath($"Packages/com.meta.xr.mrutilitykit/Plugins/{folder}/libmrutilitykitshared.dylib");
#elif UNITY_ANDROID
            var path = "libmrutilitykitshared.so";
#endif
            _nativeLibraryPtr = GetDllHandle(path);

            if (_nativeLibraryPtr == IntPtr.Zero)
            {
                Debug.LogError($"Failed to load {path}");
            }

            MRUKNativeFuncs.LoadNativeFunctions();
        }

        internal static void FreeMRUKSharedLibrary()
        {
            MRUKNativeFuncs.UnloadNativeFunctions();

            if (_nativeLibraryPtr == IntPtr.Zero)
            {
                return;
            }

            if (!FreeDllHandle(_nativeLibraryPtr))
            {
                Debug.LogError("Failed to free mr utility kit shared library");
            }

            _nativeLibraryPtr = IntPtr.Zero;
        }

        internal static T LoadFunction<T>(string name)
        {
            if (_nativeLibraryPtr == IntPtr.Zero)
            {
                Debug.LogWarning($"Failed to load {name} because mr utility kit shared library is not loaded");
                return default;
            }
            IntPtr funcPtr = GetDllExport(_nativeLibraryPtr, name);
            if (funcPtr == IntPtr.Zero)
            {
                Debug.LogWarning($"Could not find {name} in mr utility kit shared library");
                return default;
            }
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
    }
}
