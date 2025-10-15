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
using System.Collections.Generic;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    ///     A struct that can filter certain labels. The default is
    ///     to allow all labels.
    /// </summary>
    public struct LabelFilter
    {
        /// <summary>
        /// A list of labels to include, null is equivalent to all labels.
        /// </summary>
        public MRUKAnchor.SceneLabels? SceneLabels;

        /// <summary>
        /// Optional enum flags representing component types to include, null is equivalent to all component types.
        /// </summary>
        public MRUKAnchor.ComponentType? ComponentTypes;

        /// <summary>
        /// Creates a label filter that includes only the specified labels and component types.
        /// </summary>
        /// <param name="labelFlags">Enum flags representing labels to include.</param>
        /// <param name="componentTypes">Enum flags representing component types to include.</param>
        public LabelFilter(MRUKAnchor.SceneLabels? labelFlags = null, MRUKAnchor.ComponentType? componentTypes = null)
        {
            SceneLabels = labelFlags;
            ComponentTypes = componentTypes;
        }


        /// <summary>
        /// Creates a label filter that includes the specified labels. This method is obsolete.
        /// Use the enum-based 'Included' method instead for type safety and better performance.
        /// </summary>
        /// <param name="included">A list of labels to include, specified as strings.</param>
        /// <returns>A LabelFilter that includes the specified labels.</returns>
        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public static LabelFilter Included(List<string> included) => Included(Utilities.StringLabelsToEnum(included));

        /// <summary>
        /// Creates a label filter that excludes the specified labels. This method is obsolete.
        /// Use the enum-based 'Excluded' method instead for type safety and better performance.
        /// </summary>
        /// <param name="excluded">A list of labels to exclude, specified as strings.</param>
        /// <returns>A LabelFilter that excludes the specified labels.</returns>
        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public static LabelFilter Excluded(List<string> excluded) => Excluded(Utilities.StringLabelsToEnum(excluded));

        /// <summary>
        /// Creates a label filter from the specified enum labels. This method is obsolete.
        /// Use the 'Included' method instead which directly accepts an enum of labels.
        /// </summary>
        /// <param name="labels">The labels specified as an enum.</param>
        /// <returns>A LabelFilter that includes the specified labels.</returns>
        /// <see cref="OVRSemanticLabels.DeprecationMessage" />
        [Obsolete("Use '" + nameof(Included) + "()' instead.")]
        public static LabelFilter FromEnum(MRUKAnchor.SceneLabels labels) => Included(labels);

        /// <summary>
        /// Checks if the specified string labels pass the current filter. This method is obsolete.
        /// Use the enum-based 'PassesFilter' method instead for type safety and better performance.
        /// </summary>
        /// <param name="labels">A list of labels to check, specified as strings.</param>
        /// <returns>True if the labels pass the filter, false otherwise.</returns>
        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public bool PassesFilter(List<string> labels) => PassesFilter(Utilities.StringLabelsToEnum(labels));

        /// <summary>
        /// Creates a label filter that includes only the specified labels.
        /// </summary>
        /// <param name="labelFlags">Enum flags representing labels to include.</param>
        /// <returns>A LabelFilter that includes specified labels.</returns>
        [Obsolete("Use `new LabelFilter(labelFlags)` instead")]
        public static LabelFilter Included(MRUKAnchor.SceneLabels labelFlags) =>
            new(labelFlags, null);

        /// <summary>
        /// Creates a label filter that excludes the specified labels.
        /// </summary>
        /// <param name="labelFlags">Enum flags representing labels to exclude.</param>
        /// <returns>A LabelFilter that excludes specified labels.</returns>
        [Obsolete("Use `new LabelFilter(~labelFlags)` instead")]
        public static LabelFilter Excluded(MRUKAnchor.SceneLabels labelFlags) =>
            new() { SceneLabels = ~labelFlags, ComponentTypes = null };

        /// <summary>
        /// Checks if the given enum of labels passes the filter.
        /// </summary>
        /// <param name="labelFlags">Enum flags representing labels to check.</param>
        /// <returns>True if the labels pass the filter, false otherwise.</returns>
        public bool PassesFilter(MRUKAnchor.SceneLabels labelFlags)
        {
            if (SceneLabels.HasValue)
            {
                return (SceneLabels.Value & labelFlags) != 0;
            }

            return true;
        }
    }
}
