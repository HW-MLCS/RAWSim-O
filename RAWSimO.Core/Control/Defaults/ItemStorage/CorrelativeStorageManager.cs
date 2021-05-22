using RAWSimO.Core.Configurations;
using RAWSimO.Core.Elements;
using RAWSimO.Core.Interfaces;
using RAWSimO.Core.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Control.Defaults.ItemStorage
{
    /// <summary>
    /// Creates a correlative storage manager that aims to assign bundles in a family based manner.
    /// </summary>
    public class CorrelativeStorageManager : ItemStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public CorrelativeStorageManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.ItemStorageConfig as CorrelativeItemStorageConfiguration;
        }
        /// <summary>
        /// Selects a pod for a bundle generated during initialization.
        /// </summary>
        /// <param name="instance">The active instance.</param>
        /// <param name="bundle">The bundle to assign to a pod.</param>
        /// <returns>The selected pod.</returns>
        public override Pod SelectPodForInititalInventory(Instance instance, ItemBundle bundle)
        {
            // Add to a pod with similar content
            Pod pod = instance.Pods
                    .Where(b => b.FitsForReservation(bundle))
                    .OrderByDescending(p => p.ItemDescriptionsContained.Sum(containedItem => instance.FrequencyTracker.GetMeasuredFrequency(bundle.ItemDescription, containedItem)))
                                        // GetMeasuredFrequency -> new bundle과 기존의 containedItem 간의 상관관계 전부다 조사. 이를 내림차순으로 정렬(OrderByDescending)
                                /// <summary>
                                /// Returns the measured combined frequency of the given item type tuple. This value is updated throughout the simulation.
                                // / </summary>
                                // / <param name="item1">The first part of the item tuple.</param>
                                // / <param name="item2">The second part of the item tuple.</param>
                                // / <returns>The combined frequency of both items. This is a value between 0 and 1.</returns>
                                // public double GetMeasuredFrequency(ItemDescription item1, ItemDescription item2)
                                // {
                                //     if (!_combinedTracking)
                                //         throw new InvalidOperationException("Combined frequency tracking is disabled!");
                                //     if (_combinedItemFrequencies.ContainsKey(item1, item2))
                                //         return _combinedItemFrequencies[item1, item2];
                                //     else
                                //         return 0;
                                // }
                    .First();
                                // 상관관계 수치 중 제일 높은 Pod을 선정함(First())
                                // 우리는 새로 들어온 bundle이랑 정확하게 일치하는 pod을 찾는 것
            return pod;
        }
        /// <summary>
        /// The config of this controller.
        /// </summary>
        private CorrelativeItemStorageConfiguration _config;

        /// <summary>
        /// Retrieves the threshold value above which buffered decisions for that respective pod are submitted to the system.
        /// </summary>
        /// <param name="pod">The pod to get the threshold value for.</param>
        /// <returns>The threshold value above which buffered decisions are submitted. Use 0 to immediately submit decisions.</returns>
        protected override double GetStorageBufferThreshold(Pod pod) { return _config.BufferThreshold; }
        /// <summary>
        /// Retrieves the time after which buffered bundles will be allocated even if they do not meet the threshold criterion.
        /// </summary>
        /// <param name="pod">The pod to get the timeout value for.</param>
        /// <returns>The buffer timeout.</returns>
        protected override double GetStorageBufferTimeout(Pod pod) { return _config.BufferTimeout; }

        /// <summary>
        /// This is called to decide about potentially pending bundles.
        /// This method is being timed for statistical purposes and is also ONLY called when <code>SituationInvestigated</code> is <code>false</code>.
        /// Hence, set the field accordingly to react on events not tracked by this outer skeleton.
        /// </summary>
        protected override void DecideAboutPendingBundles()
        {
            foreach (var bundle in _pendingBundles.ToArray())
            {
                // Find a pod
                Pod chosenPod = Instance.Pods
                    .Where(b => b.FitsForReservation(bundle))
                    .OrderByDescending(p => p.ItemDescriptionsContained.Sum(containedItem => Instance.FrequencyTracker.GetMeasuredFrequency(bundle.ItemDescription, containedItem)))
                    .FirstOrDefault();
                // If we found a pod, assign the bundle to it
                if (chosenPod != null)
                    AddToReadyList(bundle, chosenPod);
            }
        }

        #region IOptimize Members

        /// <summary>
        /// Signals the current time to the mechanism. The mechanism can decide to block the simulation thread in order consume remaining real-time.
        /// </summary>
        /// <param name="currentTime">The current simulation time.</param>
        public override void SignalCurrentTime(double currentTime) { /* Ignore since this simple manager is always ready. */ }

        #endregion
    }
}
