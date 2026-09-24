using System.Collections.Generic;
using UnityEngine;

namespace Farm.Actors
{
    public sealed class MarketLayout : MonoBehaviour
    {
        private readonly List<Transform> customerPoints = new List<Transform>();
        private readonly List<Transform> workerPoints = new List<Transform>();

        public Transform CustomerStart { get; private set; }
        public Transform CustomerEnd { get; private set; }
        public Transform WorkerOrigin { get; private set; }
        public int SlotCount => customerPoints.Count;

        public bool HasRequiredAnchors
        {
            get
            {
                var docks = transform.Find("Dock");
                if (transform.Find("CustomerStart") == null || transform.Find("CustomerEnd") == null ||
                    transform.Find("DeliveryEnd") == null || docks == null || docks.childCount == 0) return false;
                for (var i = 0; i < docks.childCount; i++)
                    if (docks.GetChild(i).Find("Delivery") == null) return false;
                return true;
            }
        }

        public bool Initialize()
        {
            customerPoints.Clear();
            workerPoints.Clear();
            CustomerStart = transform.Find("CustomerStart");
            CustomerEnd = transform.Find("CustomerEnd");
            WorkerOrigin = transform.Find("DeliveryEnd");
            var docks = transform.Find("Dock");
            if (CustomerStart == null || CustomerEnd == null || WorkerOrigin == null || docks == null)
                return false;

            for (var i = 0; i < docks.childCount; i++)
            {
                var dock = docks.GetChild(i);
                var workerPoint = dock.Find("Delivery");
                if (workerPoint == null) return false;
                customerPoints.Add(dock);
                workerPoints.Add(workerPoint);
            }
            return customerPoints.Count > 0;
        }

        public Transform CustomerPoint(int slotId) => customerPoints[slotId];
        public Transform WorkerPoint(int slotId) => workerPoints[slotId];
    }
}
