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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{
    partial class MRUK
    {
        partial class MRUKSettings
        {
            /// <summary>
            /// The requested configuration of the tracking service.
            /// </summary>
            /// <remarks>
            /// This property represents the requested tracker configuration (which types of trackables to track). It is possible that some
            /// configuration settings may not be satisfied (for example, due to lack of device support). <see cref="MRUK.TrackerConfiguration"/>
            /// represents the true state of the system.
            /// </remarks>
            [field: SerializeField, Tooltip("Settings related to trackables that are detectable in the environment at runtime.")]
            public OVRAnchor.TrackerConfiguration TrackerConfiguration { get; set; }

            /// <summary>
            /// Invoked when a newly detected trackable has been localized.
            /// </summary>
            /// <remarks>
            /// When a new <see cref="OVRAnchor"/> has been detected and localized, a new `GameObject` with a <see cref="MRUKTrackable"/> is created
            /// to represent it. Its transform is set, and then this event is invoked.
            ///
            /// Subscribe to this event to add additional child GameObjects or further customize the behavior.
            ///
            /// <example>
            /// This example shows how to create a MonoBehaviour that instantiates a custom prefab:
            /// <code><![CDATA[
            /// class MyCustomManager : MonoBehaviour
            /// {
            ///     public GameObject Prefab;
            ///
            ///     public void OnTrackableAdded(MRUKTrackable trackable)
            ///     {
            ///         Instantiate(Prefab, trackable.transform);
            ///     }
            /// }
            /// ]]></code>
            /// </example>
            /// </remarks>
            [field: SerializeField, Tooltip("Invoked after a newly detected anchor has been localized.")]
            public UnityEvent<MRUKTrackable> TrackableAdded { get; private set; } = new();

            /// <summary>
            /// Invoked when an existing trackable is no longer detected by the runtime.
            /// </summary>
            /// <remarks>
            /// When an anchor is removed, no action is taken by default. The <see cref="MRUKTrackable"/>, if any, is not destroyed or deactivated.
            /// Subscribe to this event to change this behavior.
            ///
            /// Once this event has been invoked, the <see cref="MRUKTrackable"/>'s anchor (<see cref="MRUKTrackable.Anchor"/>) is no longer valid.
            /// </remarks>
            [field: SerializeField, Tooltip("The event is invoked when an anchor is removed.")]
            public UnityEvent<MRUKTrackable> TrackableRemoved { get; private set; } = new();
        }

        /// <summary>
        /// The current configuration for the tracking service.
        /// </summary>
        /// <remarks>
        /// To request a particular configuration, set the desired values in <see cref="MRUKSettings.TrackerConfiguration"/>.
        /// This property represents the true state of the system.
        /// This may differ from what was requested with <see cref="MRUKSettings.TrackerConfiguration"/> if, for example, some types of trackables are not supported on the current device.
        /// </remarks>
        public OVRAnchor.TrackerConfiguration TrackerConfiguration => _tracker?.Configuration ?? default;

        /// <summary>
        /// Get all the trackables that have been detected so far.
        /// </summary>
        /// <param name="trackables">The list to populate with the trackables. The list is cleared before adding any elements.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="trackables"/> is `null`.</exception>
        public void GetTrackables(List<MRUKTrackable> trackables)
        {
            if (trackables == null)
            {
                throw new ArgumentNullException(nameof(trackables));
            }

            trackables.Clear();
            foreach (var transform in _trackableTransforms.Values)
            {
                if (transform && transform.TryGetComponent<MRUKTrackable>(out var trackable))
                {
                    trackables.Add(trackable);
                }
            }
        }

        private readonly OVRAnchor.Tracker _tracker = new();

        private Coroutine _trackerCoroutine;

        private enum TrackableState
        {
            PendingLocalization,
            InstanceDestroyed,
            Instantiated,
            LocalizationFailed,
        }

        private readonly Dictionary<OVRAnchor, TrackableState> _trackableStates = new();

        private readonly Dictionary<OVRAnchor, Transform> _trackableTransforms = new();

        private void OnEnable()
        {
            _trackerCoroutine = StartCoroutine(TrackerCoroutine());
        }

        private void UpdateTrackables()
        {
            // Remove Transforms that may have been destroyed by user code
            using (new OVRObjectPool.ListScope<OVRAnchor>(out var keysToRemove))
            {
                foreach (var (anchor, transform) in _trackableTransforms)
                {
                    if (transform == null)
                    {
                        keysToRemove.Add(anchor);
                    }
                }

                foreach (var anchor in keysToRemove)
                {
                    _trackableTransforms.Remove(anchor);

                    // Remember that we removed it so we don't add it back in the next update
                    _trackableStates[anchor] = TrackableState.InstanceDestroyed;

                    // Dispose the anchor so the runtime doesn't continue to track it
                    anchor.Dispose();
                }
            }

            using (new OVRObjectPool.ListScope<OVRLocatable.TrackingSpacePose>(out var poses))
            {
                OVRLocatable.UpdateSceneAnchorTransforms(_trackableTransforms, _cameraRig ? _cameraRig.trackingSpace : null, poses);

                using var poseIter = poses.GetEnumerator();
                foreach (var (_, instance) in _trackableTransforms)
                {
                    poseIter.MoveNext();
                    var pose = poseIter.Current;

                    if (instance.TryGetComponent<MRUKTrackable>(out var trackable))
                    {
                        trackable.IsTracked = pose.IsPositionTracked && pose.IsRotationTracked;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (_trackerCoroutine != null)
            {
                StopCoroutine(_trackerCoroutine);
                _trackerCoroutine = null;
            }

            _tracker.Dispose();
        }

        private async void ConfigureTrackerAndLogResult(OVRAnchor.TrackerConfiguration config)
        {
            var result = await _tracker.ConfigureAsync(config);
            if (this && enabled)
            {
                if (result.Success)
                {
                    Debug.Log($"Configured anchor trackers: {_tracker.Configuration}");
                }
                else
                {
                    Debug.LogWarning($"Unable to fully satisfy requested tracker configuration. Requested={config}, Actual={_tracker.Configuration}");
                }
            }
        }

        // 1 second because that is the approximate update time for most of our trackers
        private static readonly TimeSpan TimeBetweenFetchTrackables = TimeSpan.FromSeconds(1);

        private IEnumerator TrackerCoroutine()
        {
            var anchors = new List<OVRAnchor>();
            var removed = new HashSet<OVRAnchor>();
            var lastConfig = default(OVRAnchor.TrackerConfiguration);
            var hasScenePermission = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);

            while (enabled)
            {
                var nextFetchTime = Time.realtimeSinceStartup + TimeBetweenFetchTrackables.TotalSeconds;
                var startFrame = Time.frameCount;
                var hasScenePermissionBeenGrantedSinceLastCheck = false;
                if (!hasScenePermission && Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission))
                {
                    hasScenePermission = hasScenePermissionBeenGrantedSinceLastCheck = true;
                }

                // We should only try to set the tracker configuration if
                // 1. The requested configuration has changed since last time
                // 2. The actual tracker configuration does not match the requested one, but permissions have also changed, which may now allow one of the failing requests to succeed.
                if (lastConfig != SceneSettings.TrackerConfiguration ||
                    (hasScenePermissionBeenGrantedSinceLastCheck && _tracker.Configuration != SceneSettings.TrackerConfiguration))
                {
                    ConfigureTrackerAndLogResult(SceneSettings.TrackerConfiguration);
                    lastConfig = SceneSettings.TrackerConfiguration;
                }

                var task = _tracker.FetchTrackablesAsync(anchors);
                while (!task.IsCompleted)
                {
                    yield return null;
                    if (!enabled)
                    {
                        task.Dispose();
                        yield break;
                    }
                }

                var result = task.GetResult();
                if (result.Success)
                {
                    removed.Clear();

                    // Add all extant anchors to the "removed" list
                    foreach (var anchor in _trackableStates.Keys)
                    {
                        removed.Add(anchor);
                    }

                    foreach (var anchor in anchors)
                    {
                        if (_trackableStates.TryAdd(anchor, TrackableState.PendingLocalization))
                        {
                            if (anchor.TryGetComponent<OVRLocatable>(out var locatable))
                            {
                                LocalizeTrackable(anchor, locatable);
                            }
                        }
                        // Update the trackable if it was
                        // 1. Previously instantiated by MRUK
                        // 2. GameObject has not been destroyed (instance != null)
                        // 3. Still has an MRUKTrackable (dev or another system could have Destroyed it)
                        else if (_trackableTransforms.TryGetValue(anchor, out var instance) && instance &&
                                 instance.TryGetComponent<MRUKTrackable>(out var trackable) && trackable)
                        {
                            try
                            {
                                trackable.OnFetch();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }

                        removed.Remove(anchor);
                    }

                    // What's left in removed are no longer being reported
                    foreach (var anchor in removed)
                    {
                        _trackableStates.Remove(anchor);
                    }

                    // We are potentially calling into user code which could throw, but there's no way for
                    // the user to catch the exception(s). We want the exceptions but continue execution.
                    using (new OVRObjectPool.ListScope<MRUKTrackable>(out var removedTrackables))
                    {
                        foreach (var anchor in removed)
                        {
                            if (_trackableTransforms.Remove(anchor, out var transform) &&
                                transform && transform.TryGetComponent<MRUKTrackable>(out var trackable))
                            {
                                removedTrackables.Add(trackable);
                            }
                        }

                        foreach (var trackable in removedTrackables)
                        {
                            try
                            {
                                trackable.IsTracked = false;
                                SceneSettings.TrackableRemoved.Invoke(trackable);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }
                else
                {
#if DEVELOPMENT_BUILD
                    Debug.LogError($"{nameof(OVRAnchor.Tracker.FetchTrackablesAsync)} failed: {result.Status}");
#endif
                }

                // Wait until the next query time
                while (enabled && (startFrame == Time.frameCount || // Always wait at least one frame
                                   Time.realtimeSinceStartup < nextFetchTime))
                {
                    yield return null;
                }
            }
        }

        private async void LocalizeTrackable(OVRAnchor anchor, OVRLocatable locatable)
        {
            if (await locatable.SetEnabledAsync(true))
            {
                while (this) // In case MRUK is Destroy'd while waiting
                {
                    if (!_trackableStates.TryGetValue(anchor, out var state) ||
                        state != TrackableState.PendingLocalization)
                    {
                        // State changed while we were awaiting localization
                        return;
                    }

                    if (!enabled)
                    {
                        // MRUK was disabled while awaiting localization; remove state tracking so that we
                        // pick it up later if we are re-enabled.
                        _trackableStates.Remove(anchor);
                    }

                    // Sometimes, we don't always get an initial pose (e.g., if the HMD is doff'd)
                    // so keep trying until we get one.
                    if (locatable.TryGetSceneAnchorPose(out var trackingSpacePose) &&
                        trackingSpacePose is { Position: not null, Rotation: not null })
                    {
                        var go = new GameObject($"Trackable({anchor.GetTrackableType()}) {anchor}");
                        go.transform.SetParent(_cameraRig.trackingSpace, worldPositionStays: false);
                        go.transform.SetLocalPositionAndRotation(trackingSpacePose.Position.Value, trackingSpacePose.Rotation.Value);
                        _trackableTransforms[anchor] = go.transform;

                        var trackable = go.AddComponent<MRUKTrackable>();
                        trackable.OnInstantiate(anchor);

                        _trackableStates[anchor] = TrackableState.Instantiated;

                        // Notify user
                        try
                        {
                            SceneSettings.TrackableAdded.Invoke(trackable);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }

                        return;
                    }

                    await Task.Yield();
                }
            }
            else // Localization failed.
            {
                if (this)
                {
                    _trackableStates[anchor] = TrackableState.LocalizationFailed;
                }

                Debug.LogError($"Unable to localize anchor {anchor}. Will not create a GameObject to represent it.");
            }

            // MRUK has either been destroyed, or an unexpected error occurred.
            // Dispose of the anchor to avoid wasting system resources on it.
            anchor.Dispose();
        }
    }
}
