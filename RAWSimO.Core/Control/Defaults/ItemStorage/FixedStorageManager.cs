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
    /// Creates a fixed storage manager that aims to assign bundles in a pre-defined(fixed) based manner.
    /// </summary>
    public class FixedStorageManager : ItemStorageManager
    {
        /// <summary>
        /// Creates a new instance of this manager.
        /// </summary>
        /// <param name="instance">The instance this manager belongs to.</param>
        public FixedStorageManager(Instance instance) : base(instance)
        {
            _config = instance.ControllerConfig.ItemStorageConfig as FixedItemStorageConfiguration;
        }
        /// <summary>
        /// Selects a pod for a bundle generated during initialization.
        /// </summary>
        /// <param name="instance">The active instance.</param>
        /// <param name="bundle">The bundle to assign to a pod.</param>
        /// <returns>The selected pod.</returns>
        public override Pod SelectPodForInititalInventory(Instance instance, ItemBundle bundle)
        {
            // Add to a pod with same content
            Pod pod = instance.Pods
                    .Where(b => b.FitsForReservation(bundle)) // bundle -> new / instance -> 기존 
                        //    public bool FitsForReservation(ItemBundle bundle) { return CapacityInUse + CapacityReserved + bundle.BundleWeight <= Capacity; }
                        // Capacity 체크해서, 담길수 있는지 1차로 체크
                    .OrderByDescending(p => p.ItemDescriptionsContained.Sum(containedItem => instance.FrequencyTracker.CheckSameItem(bundle.ItemDescription, containedItem)))
                        // 모든 Pod의 contained Item에 대해서, 새로 들어오는 bundle과 같은지 check (Item Description으로 체크. Need to check the type of "Item Description")
                        // 같으면 1, 그 외에는 0 
                    .First();
                        // 가장 첫번째 pod 고름.
                        // TODO. 그 중에서, 한번더 selection. ex)거리순으로 한번 더 정렬. or randomly 선택. 

                        // (기존) 거리에 따라 정렬하는 코드 (Tier까지 고려해서 판단.)
                    // .OrderBy(b =>
                    // b.InUse ?
                    // Instance.WrongTierPenaltyDistance + Distances.CalculateEuclid(_bundleToStation[bundle], b, Instance.WrongTierPenaltyDistance) :
                    // Distances.CalculateEuclid(_bundleToStation[bundle], b, Instance.WrongTierPenaltyDistance))
                        // Bundle이 assign된 station <-> 모든 pod 간의 거리 계산
                        // 모든 거리 중 가장 '작은' 값 선정
            return pod;

        }
        /// <summary>
        /// The config of this controller.
        /// </summary>
        private FixedItemStorageConfiguration _config;

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
                    .OrderByDescending(p => p.ItemDescriptionsContained.Sum(containedItem => Instance.FrequencyTracker.CheckSameItem(bundle.ItemDescription, containedItem)))
                    .FirstOrDefault();

                // // Find a pod -> based closetLocation
                // Pod chosenPod = Instance.Pods
                //     .Where(b => b.FitsForReservation(bundle))
                //     .OrderBy(b =>
                //         b.InUse ?
                //         Instance.WrongTierPenaltyDistance + Distances.CalculateEuclid(_bundleToStation[bundle], b, Instance.WrongTierPenaltyDistance) :
                //         Distances.CalculateEuclid(_bundleToStation[bundle], b, Instance.WrongTierPenaltyDistance))
                //     .FirstOrDefault();

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
