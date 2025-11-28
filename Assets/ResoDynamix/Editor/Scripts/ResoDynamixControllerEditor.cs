#if UNITY_EDITOR
using ResoDynamix.Runtime.Scripts;
using UnityEngine;
using UnityEditor;

namespace ResoDynamix
{
    [CustomEditor(typeof(ResoDynamixController))]
    public class ResoDynamixControllerEditor : Editor
    {
        private SerializedProperty _baseCameraProp;
        private SerializedProperty _setRenderTextureToBaseCameraOutputProp;

        // Get SerializedProperty on initialization
        private void OnEnable()
        {
            _baseCameraProp = serializedObject.FindProperty("_baseCamera");
            _setRenderTextureToBaseCameraOutputProp = serializedObject.FindProperty("setRenderTextureToBaseCameraOutput");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var baseCamera = _baseCameraProp.objectReferenceValue as Camera;
            if (!baseCamera)
            {
                return;
            }

            var targetScript = (ResoDynamixController)target;
            var usingCameraStack = targetScript.UsingCameraStack;
            if (_setRenderTextureToBaseCameraOutputProp.boolValue && usingCameraStack)
            {
                EditorGUILayout.HelpBox("Warning! This feature will be disabled when BaseCamera uses CameraStack!", MessageType.Warning);
                Debug.LogWarning("Warning! The SetRenderTextureToOutput feature will be disabled when BaseCamera uses CameraStack!", target);
            }

            if (Application.isPlaying)
            {
                return;
            }
            if (_setRenderTextureToBaseCameraOutputProp.boolValue && baseCamera.targetTexture)
            {
                EditorGUILayout.HelpBox("Warning! BaseCamera's OutputTexture setting will be overwritten!", MessageType.Warning);
                Debug.LogWarning("Warning! BaseCamera's OutputTexture setting will be overwritten by ResoDynamixController!", target);
            }
        }
    }
}
#endif