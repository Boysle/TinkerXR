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

using UnityEngine;

namespace Meta.XR.MRUtilityKit.Extensions
{
    internal static class Vector2Extensions
    {
        internal static Vector2 Floor(this Vector2 a)
        {
            return new Vector2(Mathf.Floor(a.x), Mathf.Floor(a.y));
        }

        internal static Vector2 Frac(this Vector2 a)
        {
            return new Vector2(a.x - Mathf.Floor(a.x), a.y - Mathf.Floor(a.y));
        }

        internal static Vector2 Add(this Vector2 a, float b)
        {
            return new Vector3(a.x + b, a.y + b);
        }

        internal static Vector2 Abs(this Vector2 a)
        {
            return new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));
        }
    }
}
