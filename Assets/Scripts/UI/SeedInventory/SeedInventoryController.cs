using Assets.Scripts.DataModels;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts.UI.SeedInventory
{
    public class SeedInventoryController : MonoBehaviour
    {
        [Tooltip("The gameobject which all SeedInventoryDropSlots are directly nested under")]
        public GameObject seedGridLayoutParent;

        public SeedInventoryDropSlot trashSlot;

        //public VisualEffect trashParticles;
        //public Camera mainCamera;

        public static SeedInventoryController Instance;


        /// <summary>
        /// finds the first open drop slot, and puts the <paramref name="seedStack"/> in there.
        /// </summary>
        /// <param name="seedStack"></param>
        /// <returns>the object containing the seed drop slot that was modified. Null if no slot is open</returns>
        public GameObject CreateSeedStack(SeedBucketUI seedStack)
        {
            var dropSlot = GetFirstEmptyDropSlot();
            if (dropSlot == null)
            {
                return null;
            }
            dropSlot.UpdateDataModel(seedStack);
            return dropSlot.gameObject;
        }

        public SeedInventoryDropSlot GetFirstEmptyDropSlot()
        {
            foreach (Transform bucketInstance in seedGridLayoutParent.transform)
            {
                var dropSlot = bucketInstance.GetComponent<SeedInventoryDropSlot>();
                var seedModel = dropSlot.dataModel;
                if (string.IsNullOrWhiteSpace(seedModel.description) && seedModel.bucket.Empty)
                {
                    return dropSlot;
                }
            }
            if (trashSlot != null)
            {
                if (!trashSlot.dataModel.bucket.Empty)
                {
                    DoTrashEffect();
                }
                trashSlot.UpdateDataModel(new SeedBucketUI
                {
                    bucket = new SeedBucket(),
                    description = ""
                });

                return trashSlot;
            }
            return null;
        }

        private void DoTrashEffect()
        {
            var trashAnim = trashSlot.GetComponentInChildren<Animator>();
            trashAnim.SetTrigger("trashed");
        }

        private void Awake()
        {
            Instance = this;
        }
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}