
namespace GoogleARCore.ARWorld
{
    using System.Collections.Generic;
    using GoogleARCore;
    using GoogleARCore.Examples.Common;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor
    using Input = InstantPreviewInput;
#endif

    // Controls the ARWorld example
    public class ARWorldController : MonoBehaviour
    {
        // The Depth Setting Menu
        public DepthMenu DepthMenu;

        // The Instant Placement Setting Menu.
        public InstantPlacementMenu InstantPlacementMenu;

        // A prefab to place when an instant placement raycast from a user touch hits an instant placement point      
        public GameObject InstantWindow;

        // The first-person camera being used to render the passthrough camera image
        public Camera FirstPersonCamera;

        // GameObjectIndex: gameobject chosen
        // 0: chess
        // 1: fox
        // 2: cat
        public static int GameObjectIndex = 0;

        public GameObject GameObjectRobot;

        public GameObject GameObjectFox;

        public GameObject GameObjectCat;

        public GameObject GameObjectWindow;

        public Slider SliderScale;
        public Slider SliderRotate;

        public Text TextScale;

        public Text TextRotate;

        // A prefab to place when a raycast from a user touch hits a vertical plane
        private GameObject GameObjectVerticalPlanePrefab;

        // A prefab to place when a raycast from a user touch hits a horizontal plane.
        private GameObject GameObjectHorizontalPlanePrefab;

        // A prefab to place when a raycast from a user touch hits a feature point.
        private GameObject GameObjectPointPrefab;

        private GameObject InstantPlacementPrefab;

        private GameObject gameObjectTemp;

        // The rotation in degrees need to apply to prefab when it is placed.
        private const float _prefabRotation = 180.0f;

        // True if the app is in the process of quitting due to an ARCore connection error,
        // otherwise false.
        private bool _isQuitting = false;
        
        private Vector3 scale;

        // The Unity Awake() method.
        public void Awake()
        {
            // Enable ARCore to target 60fps camera capture frame rate on supported devices.
            Application.targetFrameRate = 60;

            scale = new Vector3(0.01f, 0.01f, 0.01f);
        }

        public void Update()
        {
            UpdateApplicationLifecycle();
            if(gameObjectTemp != null)
            {
                gameObjectTemp.transform.Rotate(0, SliderRotate.value, 0, Space.Self);
                gameObjectTemp.transform.localScale += SliderScale.value * scale;
            }

            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Should not handle input if the player is pointing on UI.
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            bool foundHit = false;
            if (InstantPlacementMenu.IsInstantPlacementEnabled())
            {
                foundHit = Frame.RaycastInstantPlacement(touch.position.x, touch.position.y, 1.0f, out hit);
            }
            else
            {
                TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon | TrackableHitFlags.FeaturePointWithSurfaceNormal;
                foundHit = Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit);
            }

            if (foundHit)
            {
                // Choose the gameobject to place
                if(GameObjectIndex == 0)
                {
                    GameObjectVerticalPlanePrefab = GameObjectRobot;
                    GameObjectHorizontalPlanePrefab = GameObjectRobot;
                    GameObjectPointPrefab = GameObjectRobot;
                    InstantPlacementPrefab = GameObjectRobot;
                } 

                else if(GameObjectIndex == 1)
                {
                    GameObjectVerticalPlanePrefab = GameObjectFox;
                    GameObjectHorizontalPlanePrefab = GameObjectFox;
                    GameObjectPointPrefab = GameObjectFox;
                    InstantPlacementPrefab = GameObjectFox;
                }

                else if(GameObjectIndex == 2)
                {
                    GameObjectVerticalPlanePrefab = GameObjectCat;
                    GameObjectHorizontalPlanePrefab = GameObjectCat;
                    GameObjectPointPrefab = GameObjectCat;
                    InstantPlacementPrefab = GameObjectCat;
                }

                else if(GameObjectIndex == 3)
                {
                    GameObjectVerticalPlanePrefab = GameObjectWindow;
                    GameObjectHorizontalPlanePrefab = GameObjectWindow;
                    GameObjectPointPrefab = GameObjectWindow;
                    InstantPlacementPrefab = GameObjectWindow;
                }

                // Use hit pose and camera pose to check if hittest is from the back of the plane, if it is, no need to create the anchor.
                if ((hit.Trackable is DetectedPlane) && Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position, hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.Log("Hit at back of the current DetectedPlane");
                }
                else
                {
                    if (DepthMenu != null)
                    {
                        // Show depth card window if necessary.
                        DepthMenu.ConfigureDepthBeforePlacingFirstAsset();
                    }

                    // Choose the prefab based on the Trackable that got hit.
                    GameObject prefab;
                    if (hit.Trackable is InstantPlacementPoint)
                    {
                        prefab = InstantPlacementPrefab;
                    }
                    else if (hit.Trackable is FeaturePoint)
                    {
                        prefab = GameObjectPointPrefab;
                    }
                    else if (hit.Trackable is DetectedPlane)
                    {
                        DetectedPlane detectedPlane = hit.Trackable as DetectedPlane;
                        if (detectedPlane.PlaneType == DetectedPlaneType.Vertical)
                        {
                            prefab = GameObjectVerticalPlanePrefab;
                        }
                        else
                        {
                            prefab = GameObjectHorizontalPlanePrefab;
                        }
                    }
                    else
                    {
                        prefab = GameObjectHorizontalPlanePrefab;
                    }

                    // Instantiate prefab at the hit pose.
                    gameObjectTemp = Instantiate(prefab, hit.Pose.position, hit.Pose.rotation);

                    // Compensate for the hitPose rotation facing away from the raycast
                    if(GameObjectIndex != 3)
                    {
                        gameObjectTemp.transform.Rotate(0, _prefabRotation, 0, Space.Self);
                    }
                    // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical world evolves.
                    var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                    // Make game object a child of the anchor.
                    gameObjectTemp.transform.parent = anchor.transform;

                    // Initialize Instant Placement Effect.
                    if (hit.Trackable is InstantPlacementPoint)
                    {
                        gameObjectTemp.GetComponentInChildren<InstantPlacementEffect>().InitializeWithTrackable(hit.Trackable);
                    }
                }
            }
        }

        // Check and update the application lifecycle.

        private void UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            // Only allow the screen to sleep when not tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                Screen.sleepTimeout = SleepTimeout.SystemSetting;
            }
            else
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            if (_isQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                ShowAndroidToastMessage("Camera permission is needed to run this application.");
                _isQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                _isQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
        }

        // Actually quit the application.
        private void DoQuit()
        {
            Application.Quit();
        }

        // Show an Android toast message.
        private void ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}
