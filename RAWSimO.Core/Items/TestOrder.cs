using RAWSimO.Core.Helper;
using RAWSimO.Core.Info;
using RAWSimO.Core.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace RAWSimO.Core.Items
{

    /// <summary>
    /// Defines one order.
    /// </summary>
    public class Order : IOrderInfo
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of the order.
        /// </summary>
        internal Order() { TimeStampSubmit = double.PositiveInfinity; DueTime = double.PositiveInfinity; }

        // combined_order_list 이용
        #endregion

        #region Core

        /// <summary>
        /// The overall demand quantity.
        /// </summary>
        private int _overallQuantity = 0;
        /// <summary>
        /// The quantities needed per order line.
        /// </summary>
        private Dictionary<TestItemDescription, int> _quantities = new Dictionary<TestItemDescription, int>();
        /// <summary>
        /// The requests belonging to the order lines.
        /// </summary>
        private Dictionary<TestItemDescription, HashSet<ExtractRequest>> _requests = new Dictionary<TestItemDescription, HashSet<ExtractRequest>>();
        /// <summary>
        /// The overall served demand quantity.
        /// </summary>
        private int _overallServedQuantity = 0;
        /// <summary>
        /// The quantities already fulfilled per order line.
        /// </summary>
        private Dictionary<TestItemDescription, int> _servedQuantities = new Dictionary<TestItemDescription, int>();
        /// <summary>
        /// The number of finished order lines.
        /// </summary>
        private int _servedPositions;

        /// <summary>
        /// The time this order is placed.
        /// </summary>
        public double TimeStamp { get; set; }
        /// <summary>
        /// The time stamp this order was submitted to an output-station.
        /// </summary>
        public double TimeStampSubmit { get; set; }
        /// <summary>
        /// The time this order was submitted to a queue of a station.
        /// </summary>
        public double TimeStampQueued { get; set; }
        /// <summary>
        /// The time stamp at which the order should be completed.
        /// </summary>
        public double DueTime { get; set; }
        /// <summary>
        /// Enumerates all lines of the order.
        /// </summary>
        public IEnumerable<KeyValuePair<TestItemDescription, int>> Positions { get { return _quantities; } }
        /// <summary>
        /// Gets the overall demand count of all lines of the order.
        /// </summary>
        /// <returns>The overall quantity necessary to fulfill this order.</returns>
        public int GetDemandCount() { return _overallQuantity; }
        /// <summary>
        /// Gets the overall open demand count of all lines of the order.
        /// </summary>
        /// <returns>The overall open demand of the order.</returns>
        public int GetOpenDemandCount() { return _overallQuantity - _overallServedQuantity; }
        /// <summary>
        /// Gets the given lines's demand count.
        /// </summary>
        /// <param name="item">The order line.</param>
        /// <returns>The demand of the line.</returns>
        public int GetDemandCount(TestItemDescription item) { return _quantities.ContainsKey(item) ? _quantities[item] : 0; }
        /// <summary>
        /// Enumerates all requests for picking the different items.
        /// </summary>
        public IEnumerable<ExtractRequest> Requests { get { return _requests.SelectMany(i => i.Value); } }
        /// <summary>
        /// Adds a new line to the order.
        /// </summary>
        /// <param name="TestitemDescription">The item description of the line.</param>
        /// <param name="count">The quantity.</param>
        public void AddPosition(TestItemDescription TestitemDescription, int count)
        {
            if (!_quantities.ContainsKey(TestitemDescription))
            {
                _quantities[TestitemDescription] = 0;
                _servedQuantities[TestitemDescription] = 0;
            }
            _quantities[TestitemDescription] += count;
            _overallQuantity += count;
        }
        /// <summary>
        /// Adds the request to pick the item to the order. This can be used by other components to see the particular requests' status.
        /// </summary>
        /// <param name="TestitemDescription">The SKU to add the request for.</param>
        /// <param name="request">The particular request to pick the item.</param>
        public void AddRequest(TestItemDescription TestitemDescription, ExtractRequest request)
        { if (!_requests.ContainsKey(TestitemDescription)) _requests[TestitemDescription] = new HashSet<ExtractRequest>(); _requests[TestitemDescription].Add(request); }
        /// <summary>
        /// Returns the number of units needed to fulfill the given position.
        /// </summary>
        /// <param name="item">The SKU.</param>
        /// <returns>Number of units necessary to fulfill the position corresponding to the given SKU.</returns>
        public int PositionOverallCount(TestItemDescription item) { return _quantities.ContainsKey(item) ? _quantities[item] : 0; }
        /// <summary>
        /// Returns the number of units picked so far for the given position. 한 주문에서 item 선택된 양
        /// </summary>
        /// <param name="item">The SKU.</param>
        /// <returns>Number of units picked so far for the position corresponding to the given SKU.</returns>
        public int PositionServedCount(TestItemDescription item) { return _servedQuantities.ContainsKey(item) ? _servedQuantities[item] : 0; }
        /// <summary>
        /// Fulfills one unit of this order.
        /// </summary>
        /// <param name="item">The physical unit used to fulfill a part of the order.</param>
        /// <returns>Indicates whether fulfilling one unit of the order was successful.</returns>
        public bool Serve(TestItemDescription item)
        {
            _overallServedQuantity++;
            if (_servedQuantities[item] < _quantities[item])
            {
                _servedQuantities[item]++;
                if (_servedQuantities[item] >= _quantities[item])
                {
                    _servedPositions++;
                }
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Checks whether the order is complete.
        /// </summary>
        /// <returns><code>true</code> if the order is complete, <code>false</code> otherwise.</returns>
        public bool IsCompleted() { return _servedPositions >= _quantities.Count; }

        #endregion

        #region Inherited methods

        /// <summary>
        /// Returns a simple string giving information about the object.
        /// </summary>
        /// <returns>A simple string.</returns>
        public override string ToString() { return "Order-(" + string.Join(",", _quantities.Keys.Select(p => p.ToDescriptiveString())) + ")"; }

        #endregion

        #region Helper fields

        /// <summary>
        /// A helper tag that can be used for meta information purposes by other methods.
        /// </summary>
        internal bool HelperBoolTag;

        #endregion

        #region IOrderInfo Members

        /// <summary>
        /// Gets all positions of the order.
        /// </summary>
        /// <returns>An enumeration of all item-description in this order.</returns>
        public IEnumerable<IItemDescriptionInfo> GetInfoPositions() { return _quantities.Keys; }
        /// <summary>
        /// Gets the given position's quantity.
        /// </summary>
        /// <param name="item">The position.</param>
        /// <returns>The quantity of the position.</returns>
        public int GetInfoServedCount(IItemDescriptionInfo item) { return _servedQuantities[item as TestItemDescription]; }
        /// <summary>
        /// Gets the given position's already served quantity.
        /// </summary>
        /// <param name="item">The position.</param>
        /// <returns>The already served quantity of the position.</returns>
        public int GetInfoDemandCount(IItemDescriptionInfo item) { return _quantities[item as TestItemDescription]; }
        /// <summary>
        /// Indicates whether the order is already completed.
        /// </summary>
        /// <returns><code>true</code> if the order is completed, <code>false</code> otherwise.</returns>
        public bool GetInfoIsCompleted() { return IsCompleted(); }

        #endregion
    }
}
