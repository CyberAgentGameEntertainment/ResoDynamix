using System.Collections.Generic;
using UnityEngine;

namespace ResoDynamix.Runtime.Scripts
{
    /// <summary>
    /// Dynamic resolution system
    /// </summary>
    /// <remarks>
    /// You must place one instance in the scene to use ResoDynamix.
    /// </remarks>
    public class ResoDynamix : MonoBehaviour
    {
        /// <summary>
        /// Dynamic resolution controllers
        /// </summary>
        [SerializeField] private List<ResoDynamixController> _controllers;

        public static ResoDynamix Instance { get; private set; }

        /// <summary>
        /// Add a resolution controller
        /// </summary>
        /// <param name="resoDynamixController"></param>
        public void AddController(ResoDynamixController resoDynamixController)
        {
            if (!_controllers.Contains(resoDynamixController))
                _controllers.Add(resoDynamixController);
        }

        /// <summary>
        /// Remove a dynamic resolution controller
        /// </summary>
        /// <param name="resoDynamixController"></param>
        public void RemoveController(ResoDynamixController resoDynamixController)
        {
            if (_controllers.Contains(resoDynamixController))
                _controllers.Remove(resoDynamixController);
        }

        private void Awake()
        {
            Debug.Assert(Instance == null, "Multiple instances cannot be created.");
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        /// <summary>
        /// Get the number of dynamic resolution controllers
        /// </summary>
        public int GetControllerCount()
        {
            return _controllers.Count;
        }

        /// <summary>
        /// Get a resolution controller
        /// </summary>
        /// <param name="no">Controller index</param>
        public ResoDynamixController GetController(int no)
        {
            return _controllers[no];
        }

        /// <summary>
        /// Find the controller that controls the dynamic resolution of the specified base camera or overlay camera
        /// </summary>
        public static ResoDynamixController FindController(Camera camera)
        {
            if (Instance == null || Instance._controllers == null)
                return null;
            for (int i = 0; i < Instance._controllers.Count; i++)
            {
                if (Instance._controllers[i].BaseCamera == camera)
                {
                    return Instance._controllers[i];
                }

                if (Instance._controllers[i].IsOverlayCamera(camera))
                {
                    return Instance._controllers[i];
                }

                if (Instance._controllers[i].FinalBlitCamera == camera)
                {
                    return Instance._controllers[i];
                }
            }

            return null;
        }
    }
}