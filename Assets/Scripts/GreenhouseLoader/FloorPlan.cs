using Dman.Tiling;
using Dman.Tiling.SquareCoords;
using Dman.Tiling.TileSets;
using Dman.Utilities;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.GreenhouseLoader
{
    [RequireComponent(typeof(UniversalCoordinateSystemMembers))]
    [ExecuteInEditMode]
    public class FloorPlan : MonoBehaviour
    {
        public RangePositioner coordinatePositioner;
        public MemberSpawner[] memberSpawners;
        public RectCoordinateRange floorPlanSize;
        public FloorTiles floorTiles;
        private UniversalCoordinateSystemMembers tiles => GetComponent<UniversalCoordinateSystemMembers>();


        public Object EditorTriggeredRebuildFloor()
        {
            var modifiedObject = GenerateFloorPlan();
            gameObject.DestroyAllChildren();
            GetComponent<GreenhouseBuilder>().RebuildTiles();
            SpawnMembers();

            return modifiedObject;
        }

        public Object GenerateFloorPlan()
        {
            var generatedMembers = floorTiles.GenerateFloorPlan();
            if (generatedMembers.Any(x => x.Value == null))
            {
                throw new System.Exception("null tile detected, this is not a valid floor plan.");
            }
            tiles.SetTileDataInEditMode(TileMembersSaveObject.FromTileTypeDictionary(generatedMembers));
            return tiles;
        }

        public void SpawnMembers()
        {
            foreach (var memberSpawner in memberSpawners)
            {
                memberSpawner.SpawnMembers(transform);
            }
        }

        public float2 GetLocalPoint(UniversalCoordinate coord)
        {
            return coordinatePositioner.TransformCoordinate(coord);
        }

        public UniversalCoordinate? GetHoveredCoordinate()
        {
            var plane = new Plane(
                transform.TransformDirection(Vector3.up),
                transform.TransformPoint(new Vector3(0, 0.25f, 0)));
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out var enter))
            {
                //Get the point that is clicked
                Vector3 hitPoint = ray.GetPoint(enter);

                var localPoint = transform.InverseTransformPoint(hitPoint);
                return coordinatePositioner.InverseTransformCoordinate(new float2(localPoint.x, localPoint.z));
            }
            return null;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}